using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Options;
using OsuNews.Help;
using OsuNews.Osu.Models;
using OsuNews.Osu.Models.Set;

namespace OsuNews.Osu;

class AccessTokenData(string token, DateTimeOffset expireDate)
{
    public string Token { get; } = token;
    public DateTimeOffset ExpireDate { get; } = expireDate;
}

public class CooldownException : Exception
{
}

public class OsuApi : IDisposable
{
    // эээ)
    public const int TagCountsToCount = 5;

    private const string AccessTokenDataFile = "AccessToken";
    private const string RefreshTokenDataFile = "RefreshToken";

    private readonly ILogger<OsuApi> _logger;
    private readonly OsuConfig _config;
    private readonly AppConfig _appConfig;

    private readonly HttpClient _client;

    private string? _actualRefreshToken;
    private AccessTokenData? _accessTokenData;

    private readonly string _accessTokenDataFilePath;
    private readonly string _refreshTokenDataFilePath;

    private readonly Lock _tokenUpdateLocker = new();
    private readonly TimeSpan _tokenUpdateCooldown = TimeSpan.FromMinutes(1);
    private TaskCompletionSource? _tokenUpdateTcs = null;
    private DateTimeOffset? _lastTokenUpdateDate = null;

    public OsuApi(IOptions<AppConfig> appOptions, IOptions<OsuConfig> options, ILogger<OsuApi> logger)
    {
        _logger = logger;
        _config = options.Value;
        _appConfig = appOptions.Value;

        _client = new HttpClient();

        _accessTokenDataFilePath = Path.Combine(appOptions.Value.DataPath, AccessTokenDataFile);
        _refreshTokenDataFilePath = Path.Combine(appOptions.Value.DataPath, RefreshTokenDataFile);
    }

    // TODO это должно быть не тут, но блин блять
    public async Task<OsuTagData[]?> LoadTagsAsync(ulong beatmapSetId, ulong beatmapId)
    {
        try
        {
            OsuBeatmapSet beatmapSet = await GetBeatmapSetAsync(beatmapSetId);

            OsuBeatmap? map = beatmapSet.Beatmaps.FirstOrDefault(b => b.Id == beatmapId);

            if (map == null)
            {
                _logger.LogWarning("В сете не нашлось карты, лол");
                return null;
            }

            return map.TagIds
                .Select(tagId =>
                    new
                    {
                        tagId,
                        relatedTag = beatmapSet.RelatedTags.FirstOrDefault(related => related.Id == tagId.TagId)
                    })
                .Where(an =>
                {
                    if (an.relatedTag == null)
                    {
                        _logger.LogWarning("Не нашли рилейтед тег, ого {id}", an.tagId.TagId);
                        return false;
                    }

                    return true;
                })
                .Select(an => new OsuTagData(an.tagId.TagId, an.relatedTag.Name, an.tagId.Count))
                .ToArray();
        }
        catch (Exception e)
        {
            if (e is JsonException jsonException)
            {
                string path = ExceptionLogging.GeneratePath(_appConfig.DataPath);
                await ExceptionLogging.WriteExceptionDataAsync(jsonException, path);

                _logger.LogWarning(e,
                    "Не удалось пропарсить ответ при попытке загрузить сет, информация лежит в {path}", path);
            }
            else if (e is not (HttpRequestException or TaskCanceledException))
            {
                _logger.LogWarning(e, "Специфичная ошибка при попытке загрузить сет");
            }
            else
            {
                _logger.LogWarning(e, "Не удалось загрузить сет.");
            }

            return null;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="System.Net.Http.HttpRequestException">Если провалился запрос. Такое бывает.</exception>
    /// <exception cref="System.Threading.Tasks.TaskCanceledException">Бывает...</exception>
    private async Task<RefreshResponse> MakeTokenAsync()
    {
        if (_actualRefreshToken == null)
        {
            if (File.Exists(_refreshTokenDataFilePath))
            {
                _logger.LogDebug("Читаем токен с файла");
                _actualRefreshToken = await File.ReadAllTextAsync(_refreshTokenDataFilePath);
            }
            else
            {
                _logger.LogDebug("Берём токен с конфига");
                _actualRefreshToken = _config.RefreshToken;
            }
        }

        _logger.LogDebug("Обновляем токен...");

        using var message = new HttpRequestMessage(HttpMethod.Post, "https://osu.ppy.sh/oauth/token");

        message.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _config.ClientId),
            new KeyValuePair<string, string>("client_secret", _config.Secret),
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", _actualRefreshToken)
        });

        HttpResponseMessage responseMessage =
            await _client.SendAsync(message, HttpCompletionOption.ResponseContentRead);

        responseMessage.EnsureSuccessStatusCode();

        var responseContent = await responseMessage.Content.ReadFromJsonAsync<RefreshResponse>();

        _logger.LogDebug("Пишем токен...");
        _actualRefreshToken = responseContent.RefreshToken;
        await File.WriteAllTextAsync(_refreshTokenDataFilePath, responseContent.RefreshToken);

        _logger.LogDebug("Обновили токен.");

        return responseContent;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="System.Net.Http.HttpRequestException">Если провалился запрос. Такое бывает.</exception>
    /// <exception cref="System.Threading.Tasks.TaskCanceledException">Такое бывает.</exception>
    /// <exception cref="CooldownException">Если кд на обновление.</exception>
    public async Task UpdateTokenAsync()
    {
        if (_accessTokenData == null)
        {
            if (File.Exists(_accessTokenDataFilePath))
            {
                try
                {
                    string fileContent = await File.ReadAllTextAsync(_accessTokenDataFilePath);
                    _accessTokenData = JsonSerializer.Deserialize<AccessTokenData>(fileContent);

                    _logger.LogInformation("Прочитали асец токен с файла");
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Не удалось прочитац асец конфиг");
                }
            }
        }

        TaskCompletionSource? thisTcs = null;
        while (thisTcs == null)
        {
            Task? waitTask = null;
            lock (_tokenUpdateLocker)
            {
                if (_tokenUpdateTcs != null)
                {
                    waitTask = _tokenUpdateTcs.Task;
                }
                else
                {
                    thisTcs = _tokenUpdateTcs =
                        new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                }
            }

            if (waitTask != null)
                await waitTask;
        }

        try
        {
            if (_accessTokenData != null && DateTimeOffset.UtcNow < _accessTokenData.ExpireDate)
                return;

            if (_lastTokenUpdateDate != null &&
                DateTimeOffset.UtcNow - _lastTokenUpdateDate.Value < _tokenUpdateCooldown)
            {
                throw new CooldownException();
            }

            _lastTokenUpdateDate = DateTimeOffset.UtcNow;

            RefreshResponse response = await MakeTokenAsync();

            _accessTokenData = new AccessTokenData(response.AccessToken,
                DateTimeOffset.UtcNow + TimeSpan.FromSeconds(response.ExpiresIn - 600));

            string writeContent = JsonSerializer.Serialize(_accessTokenData);
            await File.WriteAllTextAsync(_accessTokenDataFilePath, writeContent);
        }
        finally
        {
            lock (_tokenUpdateLocker)
            {
                _tokenUpdateTcs = null;
                thisTcs.SetResult();
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="System.Net.Http.HttpRequestException">Если провалился запрос. Такое бывает.</exception>
    /// <exception cref="System.Threading.Tasks.TaskCanceledException">Такое бывает.</exception>
    /// <exception cref="System.Text.Json.JsonException">Однажды и такое случилось. В <see cref="Exception.Data"/> под ключом "ResponseContent" будет лежать строка сообщения.</exception>
    public async Task<OsuGame?> GetDailyAsync()
    {
        _logger.LogDebug("Просим информацию...");

        NameValueCollection queryParams = HttpUtility.ParseQueryString("");
        queryParams["category"] = "daily_challenge";
        queryParams["mode"] = "active"; // active ended all

        using var requestMessage =
            new HttpRequestMessage(HttpMethod.Get, $"https://osu.ppy.sh/api/v2/rooms?{queryParams.ToString()}");

        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessTokenData.Token);
        requestMessage.Headers.Add("x-api-version", "20240923");

        HttpResponseMessage responseMessage =
            await _client.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead);

        // TODO нужно проверять статус код? Но не знаю, какой юзать..

        string content = await responseMessage.Content.ReadAsStringAsync();

        try
        {
            return JsonSerializer.Deserialize<OsuGame[]>(content).FirstOrDefault();
        }
        catch (JsonException jsonException)
        {
            jsonException.Data["ResponseContent"] = content;
            throw;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="System.Net.Http.HttpRequestException">Если провалился запрос. Такое бывает.</exception>
    /// <exception cref="System.Threading.Tasks.TaskCanceledException">Такое бывает.</exception>
    /// <exception cref="System.Text.Json.JsonException">Однажды и такое случилось. В <see cref="Exception.Data"/> под ключом "ResponseContent" будет лежать строка сообщения.</exception>
    public async Task<OsuBeatmapExtended> GetBeatmapAsync(ulong beatmapId)
    {
        _logger.LogDebug("Просим карту...");

        using var requestMessage =
            new HttpRequestMessage(HttpMethod.Get, $"https://osu.ppy.sh/api/v2/beatmaps/{beatmapId}");

        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessTokenData.Token);
        requestMessage.Headers.Add("x-api-version", "20240923");

        HttpResponseMessage responseMessage =
            await _client.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead);

        string contentString = await responseMessage.Content.ReadAsStringAsync();

        try
        {
            return JsonSerializer.Deserialize<OsuBeatmapExtended>(contentString);
        }
        catch (JsonException jsonException)
        {
            jsonException.Data["ResponseContent"] = contentString;
            throw;
        }
    }

    // юзертегов нет в https://osu.ppy.sh/api/v2/beatmaps/ :)
    // возможно тут есть вся инфа что и в методе выше, но мне впадлу проверять, слишком тупо всё это
    /// <summary>
    /// 
    /// </summary>
    /// <param name="beatmapSetId"></param>
    /// <returns></returns>
    /// <exception cref="System.Net.Http.HttpRequestException">Если провалился запрос. Такое бывает.</exception>
    /// <exception cref="System.Threading.Tasks.TaskCanceledException">Такое бывает.</exception>
    /// <exception cref="System.Text.Json.JsonException">Однажды и такое случилось. В <see cref="Exception.Data"/> под ключом "ResponseContent" будет лежать строка сообщения.</exception>
    public async Task<OsuBeatmapSet> GetBeatmapSetAsync(ulong beatmapSetId)
    {
        _logger.LogDebug("Просим сет...");

        using var requestMessage =
            new HttpRequestMessage(HttpMethod.Get, $"https://osu.ppy.sh/api/v2/beatmapsets/{beatmapSetId}");

        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessTokenData.Token);
        requestMessage.Headers.Add("x-api-version", "20240923");

        HttpResponseMessage responseMessage =
            await _client.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead);

        string contentString = await responseMessage.Content.ReadAsStringAsync();

        try
        {
            return JsonSerializer.Deserialize<OsuBeatmapSet>(contentString);
        }
        catch (JsonException jsonException)
        {
            jsonException.Data["ResponseContent"] = contentString;
            throw;
        }
    }

    // Этому здесь не совсем место (совсем не), но делать ещё один сервис ради этого мне впадлу.
    /// <summary>
    /// 
    /// </summary>
    /// <param name="previewUrl">в таком формате //b.ppy.sh/preview/123123.mp3</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="System.Net.Http.HttpRequestException">Если провалился запрос. Такое бывает.</exception>
    /// <exception cref="System.Threading.Tasks.TaskCanceledException">Такое бывает.</exception>
    public async Task<byte[]> DownloadPreviewAsync(string previewUrl, CancellationToken cancellationToken = default)
    {
        Uri uri = new($"https:{previewUrl}");

        byte[] content = await _client.GetByteArrayAsync(uri, cancellationToken: cancellationToken);

        return content;
    }

    // public async Task<OsuGameAttributes> RequestAttributesAsync(string accessToken, ulong beatmapId, ICollection<OsuMod> mods)
    // {
    //     _logger.LogInformation("Просим сложность...");
    //
    //     using var requestMessage =
    //         new HttpRequestMessage(HttpMethod.Post, $"https://osu.ppy.sh/api/v2/beatmaps/{beatmapId}/attributes");
    //
    //     if (mods.Count > 0)
    //     {
    //         // requestMessage.Content = JsonContent.Create(mods);
    //         requestMessage.Content = JsonContent.Create(new
    //         {
    //             mods = new []
    //             {
    //                 new {
    //                     acronym = "DA",
    //                     settings = new
    //                     {
    //                         approach_rate = 12
    //                     }
    //                 }    
    //             }
    //         });
    //     }
    //     
    //     requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    //     requestMessage.Headers.Add("x-api-version", "20240923");
    //
    //     HttpResponseMessage responseMessage = await _client.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead);
    //
    //     string responseContent = await responseMessage.Content.ReadAsStringAsync();
    //
    //     return null;
    // }

    public void Dispose()
    {
        _client.Dispose();
    }
}
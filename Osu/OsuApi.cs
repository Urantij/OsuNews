using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Web;
using Microsoft.Extensions.Options;
using OsuNews.Osu.Models;

namespace OsuNews.Osu;

public class OsuApi : IDisposable
{
    private readonly ILogger<OsuApi> _logger;
    private readonly OsuConfig _config;

    private readonly HttpClient _client;

    private string? _actualRefreshToken;

    public OsuApi(IOptions<OsuConfig> options, ILogger<OsuApi> logger)
    {
        _logger = logger;
        _config = options.Value;

        _client = new HttpClient();
    }

    private async Task<string> MakeTokenAsync()
    {
        if (_actualRefreshToken == null)
        {
            if (File.Exists(_config.RefreshTokenPath))
            {
                _logger.LogDebug("Читаем токен с файла");
                _actualRefreshToken = await File.ReadAllTextAsync(_config.RefreshTokenPath);
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
        _actualRefreshToken = responseContent.refresh_token;
        await File.WriteAllTextAsync(_config.RefreshTokenPath, responseContent.refresh_token);

        _logger.LogDebug("Обновили токен.");

        return responseContent.access_token;
    }

    public async Task<OsuApiResponse> RequestAsync()
    {
        string accessToken = await MakeTokenAsync();

        OsuGame game = await GetDailyAsync(accessToken);
        OsuBeatmapExtended map = await GetBeatmapAsync(accessToken, game.CurrentPlaylistItem.BeatmapId);

        return new OsuApiResponse(game, map);
    }

    public async Task<OsuGame> GetDailyAsync(string accessToken)
    {
        _logger.LogInformation("Просим информацию...");

        NameValueCollection queryParams = HttpUtility.ParseQueryString("");
        queryParams["category"] = "daily_challenge";
        queryParams["mode"] = "active"; // active ended all

        using var requestMessage =
            new HttpRequestMessage(HttpMethod.Get, $"https://osu.ppy.sh/api/v2/rooms?{queryParams.ToString()}");

        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        requestMessage.Headers.Add("x-api-version", "20240923");

        HttpResponseMessage responseMessage =
            await _client.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead);

        OsuGame content = (await responseMessage.Content.ReadFromJsonAsync<OsuGame[]>()).First();

        return content;
    }

    public async Task<OsuBeatmapExtended> GetBeatmapAsync(string accessToken, ulong beatmapId)
    {
        _logger.LogInformation("Просим карту...");

        using var requestMessage =
            new HttpRequestMessage(HttpMethod.Get, $"https://osu.ppy.sh/api/v2/beatmaps/{beatmapId}");

        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        requestMessage.Headers.Add("x-api-version", "20240923");

        HttpResponseMessage responseMessage =
            await _client.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead);

        return await responseMessage.Content.ReadFromJsonAsync<OsuBeatmapExtended>();
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
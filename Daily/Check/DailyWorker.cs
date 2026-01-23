using System.Text.Json;
using Microsoft.Extensions.Options;
using OsuNews.Daily.Cache;
using OsuNews.Help;
using OsuNews.Map;
using OsuNews.Map.Analyze;
using OsuNews.Map.Models;
using OsuNews.Map.Parse;
using OsuNews.Osu;
using OsuNews.Osu.Models;

namespace OsuNews.Daily.Check;

public class DailyWorker : BackgroundService
{
    private readonly OsuApi _api;
    private readonly MapDownloader _mapDownloader;
    private readonly DailyCacheStore _store;
    private readonly ILogger<DailyWorker> _logger;
    private readonly DailyConfig _config;
    private readonly AppConfig _appConfig;

    public event Action<OsuFullDailyInfo>? NewDaily;

    public DailyWorker(OsuApi api, MapDownloader mapDownloader, IOptions<AppConfig> appOptions,
        IOptions<DailyConfig> options, DailyCacheStore store,
        ILogger<DailyWorker> logger)
    {
        _api = api;
        _mapDownloader = mapDownloader;
        _store = store;
        _logger = logger;
        _config = options.Value;
        _appConfig = appOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Я не знаю, как сделать так, чтобы этот сервис запускался после мейна
        // Если не ждать, мейн может не успеть подписаться на new ивент. Не знаю.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
        catch
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _api.UpdateTokenAsync();
            }
            catch (Exception e)
            {
                if (e is not (HttpRequestException or TaskCanceledException or CooldownException)) throw;

                _logger.LogWarning(e, "Не удалось обновить токен.");

                try
                {
                    await Task.Delay(_config.FailureWaitTime, stoppingToken);
                }
                catch
                {
                    return;
                }

                continue;
            }

            OsuGame? daily;
            try
            {
                daily = await _api.GetDailyAsync();
            }
            catch (Exception e)
            {
                if (e is JsonException jsonException)
                {
                    if (jsonException.Data.Contains("ResponseContent"))
                    {
                        string? data = jsonException.Data["ResponseContent"] as string;

                        _logger.LogWarning("Сообщение от лысого вместо дейлика: \"{data}\"", data);
                    }
                    else
                    {
                        _logger.LogWarning("jsonException но даты не нашлось");
                    }
                }
                else if (e is not (HttpRequestException or TaskCanceledException)) throw;

                _logger.LogWarning(e, "Не удалось загрузить дейлик.");

                try
                {
                    await Task.Delay(_config.FailureWaitTime, stoppingToken);
                }
                catch
                {
                    return;
                }

                continue;
            }

            if (daily == null)
            {
                await Task.Delay(_config.ActiveCheck, stoppingToken);
                continue;
            }

            if (_store.LastDailyCache?.Game.Id == daily.Id)
            {
                TimeSpan wait;

                if (DateTime.UtcNow > _store.LastDailyCache.Game.EndsAt)
                {
                    wait = _config.ActiveCheck;
                }
                else
                {
                    wait = _config.PassiveCheck;

                    // Если активное время наступает раньше конца пассивной проверки
                    // Проверим при наступлении активного времени + ожидание чека

                    if (DateTime.UtcNow + wait >= _store.LastDailyCache.Game.EndsAt)
                    {
                        wait = (_store.LastDailyCache.Game.EndsAt - DateTime.UtcNow) + _config.ActiveCheck;
                    }
                }

                if (wait < _config.ActiveCheck)
                {
                    wait = _config.ActiveCheck;
                }

                try
                {
                    await Task.Delay(wait, stoppingToken);
                }
                catch
                {
                    return;
                }

                continue;
            }

            _logger.LogInformation("Новый дейлик {id}", daily.Id);

            OsuFullDailyInfo info;
            try
            {
                info = await CreateFullInfoAsync(daily, stoppingToken);
            }
            catch (Exception e)
            {
                if (e is JsonException jsonException)
                {
                    string path = ExceptionLogging.GeneratePath(_appConfig.DataPath);
                    await ExceptionLogging.WriteExceptionDataAsync(jsonException, path);

                    _logger.LogWarning(e, "Не удалось пропарсить ответ, информация лежит в {path}", path);
                }
                else if (e is not (HttpRequestException or TaskCanceledException)) throw;
                else
                {
                    _logger.LogWarning(e, "Не удалось загрузить карту.");
                }

                try
                {
                    await Task.Delay(_config.FailureWaitTime, stoppingToken);
                }
                catch
                {
                    return;
                }

                continue;
            }

            await _store.OverwriteCacheAsync(info);

            NewDaily?.Invoke(info);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="daily"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="System.Net.Http.HttpRequestException">Если провалился запрос. Такое бывает.</exception>
    /// <exception cref="System.Threading.Tasks.TaskCanceledException">Такое бывает.</exception>
    /// <exception cref="System.Text.Json.JsonException">Однажды и такое случилось. В <see cref="Exception.Data"/> под ключом "ResponseContent" будет лежать строка сообщения.</exception>
    private async Task<OsuFullDailyInfo> CreateFullInfoAsync(OsuGame daily, CancellationToken cancellationToken)
    {
        // В теории, не нужно делать все запросы заново, если провалился один.
        // Но писать лупы мне впадлу, это выглядит сильно хуже.
        OsuBeatmapExtended beatmap = await _api.GetBeatmapAsync(daily.CurrentPlaylistItem.BeatmapId);

        OsuTagData[]? tags = await _api.LoadTagsAsync(beatmap.BeatmapsetId, beatmap.Id);

        MapAnalyzeResult? analyzeResult = null;
        bool triedToAnalyze = false;
        if (_config.DoAnalyze)
        {
            triedToAnalyze = true;

            try
            {
                MapData mapData = await _mapDownloader.DownloadAsync(beatmap.Id, cancellationToken: cancellationToken);

                analyzeResult = MapAnalyzer.Analyze(mapData);
            }
            catch (BadMapException badMapException)
            {
                _logger.LogWarning(badMapException.InnerException, "Не удалось проанализировать карту.");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Не удалось загрузить карту.");
            }
        }

        byte[]? previewContent = null;
        bool triedToPreview = false;
        if (_config.AttachPreview)
        {
            triedToPreview = true;

            try
            {
                previewContent = await _api.DownloadPreviewAsync(beatmap.Beatmapset.PreviewUrl, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Не удалось скачать превью.");
            }
        }

        return new OsuFullDailyInfo(daily, beatmap, tags, DateTimeOffset.UtcNow, analyzeResult, triedToAnalyze,
            previewContent,
            triedToPreview);
    }
}
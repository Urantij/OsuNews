using System.Text.Json;
using Microsoft.Extensions.Options;
using OsuNews.Analyze;
using OsuNews.Analyze.Models;
using OsuNews.Osu;
using OsuNews.Osu.Models;

namespace OsuNews.Daily;

public class DailyWorker : BackgroundService
{
    private readonly OsuApi _api;
    private readonly MapDownloader _mapDownloader;
    private readonly ILogger<DailyWorker> _logger;
    private readonly DailyConfig _config;

    private DailyCacheInfo? _lastDailyCache;

    public event Action<OsuFullDailyInfo>? NewDaily;

    public DailyWorker(OsuApi api, MapDownloader mapDownloader, IOptions<DailyConfig> options,
        ILogger<DailyWorker> logger)
    {
        _api = api;
        _mapDownloader = mapDownloader;
        _logger = logger;
        _config = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (File.Exists(_config.CachePath))
        {
            string content = await File.ReadAllTextAsync(_config.CachePath, stoppingToken);

            _lastDailyCache = JsonSerializer.Deserialize<DailyCacheInfo>(content);
        }

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
                if (e is not (HttpRequestException or TaskCanceledException)) throw;

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
                if (e is not (HttpRequestException or TaskCanceledException)) throw;

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

            if (_lastDailyCache?.Id == daily.Id)
            {
                TimeSpan wait;

                if (DateTime.UtcNow > _lastDailyCache.EndDate)
                {
                    wait = _config.ActiveCheck;
                }
                else
                {
                    wait = _config.PassiveCheck;

                    // Если активное время наступает раньше конца пассивной проверки
                    // Проверим при наступлении активного времени + ожидание чека

                    if (DateTime.UtcNow + wait >= _lastDailyCache.EndDate)
                    {
                        wait = (_lastDailyCache.EndDate - DateTime.UtcNow) + _config.ActiveCheck;
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

            // В теории, не нужно делать все запросы заново, если провалился один.
            // Но писать лупы мне впадлу, это выглядит сильно хуже.
            OsuBeatmapExtended beatmap;
            try
            {
                beatmap = await _api.GetBeatmapAsync(daily.CurrentPlaylistItem.BeatmapId);
            }
            catch (Exception e)
            {
                if (e is not (HttpRequestException or TaskCanceledException)) throw;

                _logger.LogWarning(e, "Не удалось загрузить карту.");

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

            MapAnalyzeResult? analyzeResult = null;
            bool triedToAnalyze = false;
            if (_config.DoAnalyze)
            {
                triedToAnalyze = true;

                try
                {
                    MapData mapData = await _mapDownloader.DownloadAsync(beatmap.Id, cancellationToken: stoppingToken);

                    analyzeResult = MapAnalyzer.Analyze(mapData);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Не удалось проанализировать карту.");
                }
            }

            byte[]? previewContent = null;
            bool triedToPreview = false;
            if (_config.AttachPreview)
            {
                triedToPreview = true;

                try
                {
                    previewContent = await _api.DownloadPreviewAsync(beatmap.Beatmapset.PreviewUrl, stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Не удалось скачать превью.");
                }
            }

            OsuFullDailyInfo info = new(daily, beatmap, analyzeResult, triedToAnalyze, previewContent, triedToPreview);
            _lastDailyCache = new DailyCacheInfo(daily.Id, daily.EndsAt.ToUniversalTime());
            {
                string content = JsonSerializer.Serialize(_lastDailyCache);
                await File.WriteAllTextAsync(_config.CachePath, content, stoppingToken);
            }

            NewDaily?.Invoke(info);
        }
    }
}
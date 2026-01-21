using Microsoft.Extensions.Options;
using OsuNews.Daily.Cache;
using OsuNews.Osu;

namespace OsuNews.Daily.TagsUpdater;

public class DailyTagUpdater : BackgroundService
{
    private readonly OsuApi _api;
    private readonly DailyCacheStore _store;
    private readonly ILogger<DailyTagUpdater> _logger;
    private readonly DailyConfig _config;

    public event Action<DailyCacheInfo, OsuTagData[]>? TagsUpdated;

    public DailyTagUpdater(OsuApi api, IOptions<DailyConfig> options, DailyCacheStore store,
        ILogger<DailyTagUpdater> logger)
    {
        _api = api;
        _store = store;
        _logger = logger;
        _config = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
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
                    continue;
                }
                catch
                {
                    return;
                }
            }

            DailyCacheInfo? cache = _store.LastDailyCache;
            // такова не бывает если кеш не старый. если старый, то всё равно
            if (cache == null || cache.LatestTagsUpdate == null)
            {
                try
                {
                    await Task.Delay(_config.FailureWaitTime, stoppingToken);
                    continue;
                }
                catch
                {
                    return;
                }
            }

            TimeSpan passed = DateTimeOffset.UtcNow - cache.LatestTagsUpdate.Value;

            if (cache.Tags == null || passed < _config.TagsRecheckTime)
            {
                try
                {
                    await Task.Delay(_config.TagsRecheckTime - passed + TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }
                catch
                {
                    return;
                }
            }

            OsuTagData[]? tags = await _api.LoadTagsAsync(cache.BeatmapSetId, cache.BeatmapId);

            if (tags == null)
            {
                try
                {
                    await Task.Delay(_config.FailureWaitTime, stoppingToken);
                    continue;
                }
                catch
                {
                    return;
                }
            }

            string[] were = cache.Tags?.Where(t => t.Count > OsuApi.TagCountsToCount).Select(t => t.Name).ToArray() ??
                            [];
            string[] now = tags.Where(t => t.Count > OsuApi.TagCountsToCount).Select(t => t.Name).ToArray();

            if (now.Length == were.Length && now.Intersect(were).Count() == now.Length) continue;
            if (_store.LastDailyCache != cache) continue;

            cache.Tags = tags;
            await _store.OverwriteCacheAsync(cache);

            TagsUpdated?.Invoke(cache, tags);
        }
    }
}
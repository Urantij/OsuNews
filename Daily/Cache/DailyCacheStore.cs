using System.Text.Json;
using Microsoft.Extensions.Options;

namespace OsuNews.Daily.Cache;

/// <summary>
/// хранит дейли кеш (не деньги)
/// </summary>
public class DailyCacheStore : IHostedService
{
    private readonly ILogger<DailyCacheStore> _logger;
    private readonly DailyConfig _config;

    public DailyCacheInfo? LastDailyCache { get; private set; }

    public DailyCacheStore(IOptions<DailyConfig> options, ILogger<DailyCacheStore> logger)
    {
        _logger = logger;
        _config = options.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(_config.CachePath))
        {
            string content = await File.ReadAllTextAsync(_config.CachePath, cancellationToken);

            LastDailyCache = JsonSerializer.Deserialize<DailyCacheInfo>(content);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task OverwriteCacheAsync(DailyCacheInfo newInfo)
    {
        LastDailyCache = newInfo;
        await SaveCacheAsync(newInfo, CancellationToken.None);
    }

    private async Task SaveCacheAsync(DailyCacheInfo dailyCacheInfo, CancellationToken cancellationToken)
    {
        string content = JsonSerializer.Serialize(dailyCacheInfo);
        await File.WriteAllTextAsync(_config.CachePath, content, cancellationToken);
    }
}
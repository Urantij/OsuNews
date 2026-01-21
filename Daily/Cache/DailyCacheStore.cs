using System.Text.Json;
using Microsoft.Extensions.Options;

namespace OsuNews.Daily.Cache;

/// <summary>
/// хранит дейли кеш (не деньги)
/// </summary>
public class DailyCacheStore : IHostedService
{
    private readonly ILogger<DailyCacheStore> _logger;

    private const string FileName = "LastDailyCache.json";

    private readonly string _cachePath;

    public DailyCacheInfo? LastDailyCache { get; private set; }

    public DailyCacheStore(IOptions<AppConfig> options, ILogger<DailyCacheStore> logger)
    {
        _logger = logger;

        _cachePath = Path.Combine(options.Value.DataPath, FileName);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(_cachePath))
        {
            string content = await File.ReadAllTextAsync(_cachePath, cancellationToken);

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
        await File.WriteAllTextAsync(_cachePath, content, cancellationToken);
    }
}
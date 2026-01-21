using System.Text.Json;
using Microsoft.Extensions.Options;
using OsuNews.Osu;

namespace OsuNews.Daily.Cache;

/// <summary>
/// хранит дейли кеш (не деньги)
/// </summary>
public class DailyCacheStore : IHostedService
{
    private readonly ILogger<DailyCacheStore> _logger;

    private const string FileName = "LastDailyCache.json";
    private const string PreviewFileName = "preview";

    private readonly string _cachePath;
    private readonly string _previewPath;

    // в теории если несколько сохраняторов зайдут они могут пойти сохранять пока сохраняется и намудить говна
    // это решается тока очередью, а мне её писать впадлу. уви.
    private readonly Lock _lock = new();

    private readonly Task? _saveTask;
    public OsuFullDailyInfo? LastDailyCache { get; private set; }

    public DailyCacheStore(IOptions<AppConfig> options, ILogger<DailyCacheStore> logger)
    {
        _logger = logger;

        _cachePath = Path.Combine(options.Value.DataPath, FileName);
        _previewPath = Path.Combine(options.Value.DataPath, PreviewFileName);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(_cachePath))
        {
            string content = await File.ReadAllTextAsync(_cachePath, cancellationToken);

            byte[]? previewContent = null;
            if (File.Exists(_previewPath))
            {
                previewContent = await File.ReadAllBytesAsync(_previewPath, cancellationToken);
            }

            LastDailyCache = JsonSerializer.Deserialize<OsuFullDailyInfo>(content);
            LastDailyCache.PreviewContent = previewContent;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task OverwriteCacheAsync(OsuFullDailyInfo newInfo)
    {
        lock (_lock)
            LastDailyCache = newInfo;

        await SaveCacheAsync(newInfo, CancellationToken.None);
    }

    public async Task UpdateCacheAsync(OsuFullDailyInfo newInfo)
    {
        lock (_lock)
        {
            if (LastDailyCache != newInfo)
                return;
        }

        await SaveCacheAsync(newInfo, CancellationToken.None);
    }

    private async Task SaveCacheAsync(OsuFullDailyInfo dailyCacheInfo, CancellationToken cancellationToken)
    {
        string content = JsonSerializer.Serialize(dailyCacheInfo);
        await File.WriteAllTextAsync(_cachePath, content, cancellationToken);

        if (dailyCacheInfo.PreviewContent != null)
        {
            await File.WriteAllBytesAsync(_previewPath, dailyCacheInfo.PreviewContent, cancellationToken);
        }
        else if (File.Exists(_previewPath))
        {
            File.Delete(_previewPath);
        }
    }
}
using Microsoft.Extensions.Options;
using OsuNews.MyTube;

namespace OsuNews.VideoV;

public class VideoViewer : BackgroundService
{
    private readonly TubeApi _api;
    private readonly VideoViewerConfig _config;

    private string? _lastKnownVideoId;

    // 10000 запросов в день
    // Раз в 15 сек норм (с запасом)
    private readonly TimeSpan _checkDelay = TimeSpan.FromSeconds(15);

    public event Action<string>? NewVideoUploaded;

    public VideoViewer(TubeApi api, IOptions<VideoViewerConfig> options)
    {
        _api = api;
        _config = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (File.Exists(_config.CachePath))
        {
            _lastKnownVideoId = await File.ReadAllTextAsync(_config.CachePath, stoppingToken);
        }
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkDelay, stoppingToken);
            }
            catch
            {
                return;
            }

            string latestVideoId = await _api.RequestAsync();

            if (_lastKnownVideoId == null)
            {
                _lastKnownVideoId = latestVideoId;
                continue;
            }

            if (_lastKnownVideoId == latestVideoId)
                continue;

            _lastKnownVideoId = latestVideoId;
            await File.WriteAllTextAsync(_config.CachePath, latestVideoId, stoppingToken);

            NewVideoUploaded?.Invoke(latestVideoId);
        }
    }
}
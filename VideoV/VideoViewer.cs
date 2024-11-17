using Microsoft.Extensions.Options;
using OsuNews.MyTube;

namespace OsuNews.VideoV;

public class VideoViewer : BackgroundService
{
    private readonly TubeApi _api;
    private readonly ILogger<VideoViewer> _logger;
    private readonly VideoViewerConfig _config;

    private string? _lastKnownVideoId;

    public event Action<string>? NewVideoUploaded;

    public VideoViewer(TubeApi api, IOptions<VideoViewerConfig> options, ILogger<VideoViewer> logger)
    {
        _api = api;
        _logger = logger;
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
                await Task.Delay(_config.CheckDelay, stoppingToken);
            }
            catch
            {
                return;
            }

            string latestVideoId;
            try
            {
                latestVideoId = await _api.RequestAsync();
            }
            catch (Exception e)
            {
                if (e is not (HttpRequestException or TaskCanceledException)) throw;

                _logger.LogWarning(e, "Не удалось забрать айди последнего видео.");
                continue;
            }

            if (_lastKnownVideoId == latestVideoId)
                continue;

            bool wasNull = _lastKnownVideoId == null;

            _lastKnownVideoId = latestVideoId;
            await File.WriteAllTextAsync(_config.CachePath, latestVideoId, stoppingToken);

            if (wasNull)
                continue;

            _logger.LogInformation("Новое видево {id}", latestVideoId);

            NewVideoUploaded?.Invoke(latestVideoId);
        }
    }
}
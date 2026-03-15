using System.Net;
using Microsoft.Extensions.Options;
using OsuNews.MyTube;

namespace OsuNews.VideoV;

public class VideoViewer : BackgroundService
{
    private const string FileName = "LastVideoId";

    private readonly TubeApi _api;
    private readonly ILogger<VideoViewer> _logger;
    private readonly VideoViewerConfig _config;

    private readonly string _cachePath;

    private string? _lastKnownVideoId;

    public event Action<string>? NewVideoUploaded;

    public VideoViewer(TubeApi api, IOptions<AppConfig> appOptions, IOptions<VideoViewerConfig> options,
        ILogger<VideoViewer> logger)
    {
        _api = api;
        _logger = logger;
        _config = options.Value;

        _cachePath = Path.Combine(appOptions.Value.DataPath, FileName);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (File.Exists(_cachePath))
        {
            _lastKnownVideoId = await File.ReadAllTextAsync(_cachePath, stoppingToken);
        }

        TimeSpan additionalDelay = TimeSpan.Zero;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_config.CheckDelay + additionalDelay, stoppingToken);

                additionalDelay = TimeSpan.Zero;
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
            catch (Google.GoogleApiException apiException) when
                (apiException.HttpStatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable)
            {
                // Иногда сервис недоступен. Я не нашёл консту для этого сообщения.
                // Статус код всегда 500. Не уверен, что стоит ловить именно код 500.
                // Но с другой стороны, почему нет.
                // апд НЕ ВСЕГДА 500 )))

                additionalDelay = TimeSpan.FromMinutes(1);

                _logger.LogWarning("Не удалось забрать айди последнего видео, так как ютуб недоступен.");
                continue;
            }
            catch (Google.GoogleApiException apiException) when
                (apiException.HttpStatusCode is HttpStatusCode.NotFound)
            {
                // И такое бывает...

                additionalDelay = TimeSpan.FromMinutes(2);

                _logger.LogWarning("Не удалось забрать айди последнего видео, так как ютуб не нашёл плейлист...");
                continue;
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                additionalDelay = TimeSpan.FromSeconds(30);

                _logger.LogWarning(e, "Не удалось забрать айди последнего видео.");
                continue;
            }
            catch (Exception e)
            {
                additionalDelay = TimeSpan.FromMinutes(1);

                _logger.LogError(e, "Не удалось забрать айди последнего видео из за неизвестной ошибки.");
                continue;
            }

            if (_lastKnownVideoId == latestVideoId)
                continue;

            bool wasNull = _lastKnownVideoId == null;

            _lastKnownVideoId = latestVideoId;
            await File.WriteAllTextAsync(_cachePath, latestVideoId, stoppingToken);

            if (wasNull)
                continue;

            _logger.LogInformation("Новое видево {id}", latestVideoId);

            NewVideoUploaded?.Invoke(latestVideoId);
        }
    }
}
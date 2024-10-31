using OsuNews.Daily;
using OsuNews.Newscasters;
using OsuNews.Osu;
using OsuNews.VideoV;

namespace OsuNews.Main;

public class MainWorker : IHostedService
{
    private readonly DailyWorker? _dailyWorker;
    private readonly VideoViewer? _videoViewer;
    private readonly ILogger<MainWorker> _logger;
    private readonly List<INewscaster> _newscasters;

    public MainWorker(DailyWorker? dailyWorker, VideoViewer? videoViewer, IEnumerable<INewscaster> newscasters,
        ILogger<MainWorker> logger)
    {
        _dailyWorker = dailyWorker;
        _videoViewer = videoViewer;
        _logger = logger;
        _newscasters = newscasters.ToList();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_dailyWorker != null)
            _dailyWorker.NewDaily += DailyWorkerOnNewDaily;

        if (_videoViewer != null)
            _videoViewer.NewVideoUploaded += VideoViewerOnNewVideoUploaded;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_dailyWorker != null)
            _dailyWorker.NewDaily -= DailyWorkerOnNewDaily;

        if (_videoViewer != null)
            _videoViewer.NewVideoUploaded -= VideoViewerOnNewVideoUploaded;

        return Task.CompletedTask;
    }

    private void DailyWorkerOnNewDaily(OsuApiResponse response)
    {
        _logger.LogInformation("Сообщаем о новом дейлике...");
        StartNewscaster(newscaster => newscaster.TellThemAboutDailyAsync(response));
        _logger.LogInformation("Сообщили о новом дейлике.");
    }

    private void VideoViewerOnNewVideoUploaded(string videoId)
    {
        _logger.LogInformation("Сообщаем о новом видике...");
        StartNewscaster(newscaster => newscaster.TellThemAboutVideoAsync(videoId));
        _logger.LogInformation("Сообщили о новом видике.");
    }

    private void StartNewscaster(Func<INewscaster, Task> action)
    {
        Task.Run(async () =>
        {
            foreach (INewscaster newscaster in _newscasters)
            {
                _logger.LogInformation("Сейчас {name}", newscaster.GetType().Name);
                try
                {
                    // TODO добавить таймаут?
                    await action(newscaster);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Ошибка при попытке отправить новость.");
                }
            }
        });
    }
}
using OsuNews.Daily.Cache;
using OsuNews.Daily.Check;
using OsuNews.Daily.TagsUpdater;
using OsuNews.Newscasters;
using OsuNews.Osu;
using OsuNews.VideoV;

namespace OsuNews.Main;

public class MainWorker : IHostedService
{
    private readonly DailyWorker? _dailyWorker;
    private readonly DailyTagUpdater? _dailyTagUpdater;
    private readonly VideoViewer? _videoViewer;
    private readonly ILogger<MainWorker> _logger;
    private readonly List<INewscaster> _newscasters;

    public MainWorker(IEnumerable<INewscaster> newscasters, ILogger<MainWorker> logger,
        DailyWorker? dailyWorker = null, DailyTagUpdater? dailyTagUpdater = null,
        VideoViewer? videoViewer = null)
    {
        _dailyWorker = dailyWorker;
        _dailyTagUpdater = dailyTagUpdater;
        _videoViewer = videoViewer;
        _logger = logger;
        _newscasters = newscasters.ToList();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_dailyWorker != null)
            _dailyWorker.NewDaily += DailyWorkerOnNewDaily;

        if (_dailyTagUpdater != null)
            _dailyTagUpdater.TagsUpdated += DailyTagUpdaterOnTagsUpdated;

        if (_videoViewer != null)
            _videoViewer.NewVideoUploaded += VideoViewerOnNewVideoUploaded;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_dailyWorker != null)
            _dailyWorker.NewDaily -= DailyWorkerOnNewDaily;

        if (_dailyTagUpdater != null)
            _dailyTagUpdater.TagsUpdated -= DailyTagUpdaterOnTagsUpdated;

        if (_videoViewer != null)
            _videoViewer.NewVideoUploaded -= VideoViewerOnNewVideoUploaded;

        return Task.CompletedTask;
    }

    private void DailyWorkerOnNewDaily(OsuFullDailyInfo info)
    {
        _logger.LogInformation("Сообщаем о новом дейлике...");
        CastNewsAsync(newscaster => newscaster.TellThemAboutDailyAsync(info))
            .ContinueWith((_) => { _logger.LogInformation("Сообщили о новом дейлике."); });
    }

    private void DailyTagUpdaterOnTagsUpdated(OsuFullDailyInfo info)
    {
        _logger.LogInformation("Сообщаем о новых тегах...");

        CastNewsAsync(newscaster => newscaster.TellThemAboutUpdatedDailyAsync(info))
            .ContinueWith((_) => { _logger.LogInformation("Сообщили о новых тегах."); });
    }

    private void VideoViewerOnNewVideoUploaded(string videoId)
    {
        _logger.LogInformation("Сообщаем о новом видике...");
        CastNewsAsync(newscaster => newscaster.TellThemAboutVideoAsync(videoId))
            .ContinueWith((_) => { _logger.LogInformation("Сообщили о новом видике."); });
    }

    private Task CastNewsAsync(Func<INewscaster, Task> action)
    {
        return Task.Run(async () =>
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
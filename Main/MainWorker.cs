using OsuNews.Daily;
using OsuNews.Newscasters;
using OsuNews.Osu;
using OsuNews.Osu.Models;

namespace OsuNews.Main;

public class MainWorker : IHostedService
{
    private readonly DailyWorker _dailyWorker;
    private readonly ILogger<MainWorker> _logger;
    private readonly List<INewscaster> _newscasters;

    public MainWorker(DailyWorker dailyWorker, IEnumerable<INewscaster> newscasters, ILogger<MainWorker> logger)
    {
        _dailyWorker = dailyWorker;
        _logger = logger;
        _newscasters = newscasters.ToList();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _dailyWorker.NewDaily += DailyWorkerOnNewDaily;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _dailyWorker.NewDaily -= DailyWorkerOnNewDaily;

        return Task.CompletedTask;
    }

    private void DailyWorkerOnNewDaily(OsuApiResponse response)
    {
        Task.Run(async () =>
        {
            _logger.LogInformation("Делаем рассылку...");
            foreach (INewscaster newscaster in _newscasters)
            {
                _logger.LogInformation("Сейчас {name}", newscaster.GetType().Name);
                try
                {
                    // TODO добавить таймаут?
                    await newscaster.TellThemAsync(response);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Ошибка при попытке разослать дейлик.");
                }
            }

            _logger.LogInformation("Разослали.");
        });
    }
}
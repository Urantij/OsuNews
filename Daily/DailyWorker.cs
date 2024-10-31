using System.Text.Json;
using Microsoft.Extensions.Options;
using OsuNews.Osu;

namespace OsuNews.Daily;

public class DailyWorker : BackgroundService
{
    private readonly OsuApi _api;
    private readonly ILogger<DailyWorker> _logger;
    private readonly DailyConfig _config;

    private OsuApiResponse? _lastResponse;

    public event Action<OsuApiResponse>? NewDaily;

    public DailyWorker(OsuApi api, IOptions<DailyConfig> options, ILogger<DailyWorker> logger)
    {
        _api = api;
        _logger = logger;
        _config = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (File.Exists(_config.CachePath))
        {
            string content = await File.ReadAllTextAsync(_config.CachePath, stoppingToken);

            _lastResponse = JsonSerializer.Deserialize<OsuApiResponse>(content);
        }

        // Я не знаю, как сделать так, чтобы этот сервис запускался после мейна
        // Если не ждать, мейн может не успеть подписаться на new ивент. Не знаю.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
        catch
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            OsuApiResponse response = await _api.RequestAsync();

            if (_lastResponse?.Game.Id == response.Game.Id)
            {
                TimeSpan wait;

                if (DateTime.UtcNow.TimeOfDay > _config.ActiveTime)
                {
                    wait = _config.ActiveCheck;
                }
                else
                {
                    wait = _config.PassiveCheck;

                    // Если активное время наступает раньше конца пассивной проверки
                    // Проверим при наступлении активного времени + ожидание чека
                    if (DateTime.UtcNow.TimeOfDay + wait >= _config.ActiveTime)
                    {
                        wait = (_config.ActiveTime - DateTime.UtcNow.TimeOfDay) + _config.ActiveCheck;
                    }
                }

                try
                {
                    await Task.Delay(wait, stoppingToken);
                }
                catch
                {
                    return;
                }

                continue;
            }

            _logger.LogInformation("Новый дейлик {id}", response.Game.Id);

            _lastResponse = response;
            {
                string content = JsonSerializer.Serialize(response, new JsonSerializerOptions()
                {
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(_config.CachePath, content, stoppingToken);
            }

            NewDaily?.Invoke(response);
        }
    }
}
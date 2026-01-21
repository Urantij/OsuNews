using System.Text.Json;

namespace OsuNews.MyLittleStorage;

// По гороскопу я.. а я уже шутил
/// <summary>
/// Хранит информацию в маеньких файвах
/// </summary>
public class MyManySmallStorage<T>
    where T : class
{
    private readonly string _dirPath;

    private readonly TimeSpan _deathDoor = TimeSpan.FromDays(2);
    private readonly TimeSpan _checkTime = TimeSpan.FromMinutes(30);

    public MyManySmallStorage(string dirPath)
    {
        _dirPath = dirPath;
    }

    public void Start(CancellationToken cancellationToken)
    {
        Task.Run(async () => { await CleaningDuty(cancellationToken); }, cancellationToken);
    }

    public Task WriteAsync(string fileName, T data)
    {
        string path = Path.Combine(_dirPath, fileName);
        string content = JsonSerializer.Serialize(data);

        return File.WriteAllTextAsync(path, content);
    }

    public async Task<T?> ReadAsync(string fileName)
    {
        string path = Path.Combine(_dirPath, fileName);

        if (!File.Exists(path))
            return null;

        // не знаю тут есть самое невероятное окно возможностей умереть, так что просто так напишу это
        try
        {
            string content = await File.ReadAllTextAsync(path);

            return JsonSerializer.Deserialize<T>(content);
        }
        catch
        {
            // ignored
        }

        return null;
    }

    private async Task CleaningDuty(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            string[] files = Directory.GetFiles(_dirPath);

            foreach (string filePath in files)
            {
                FileInfo fileInfo = new(filePath);

                // надеюсь конверт нормально работает
                if (now - fileInfo.CreationTimeUtc > _deathDoor)
                {
                    File.Delete(filePath);
                }
            }

            try
            {
                await Task.Delay(_checkTime, cancellationToken);
            }
            catch
            {
                return;
            }
        }
    }
}
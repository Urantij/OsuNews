using System.Text.Json;

namespace OsuNews.MyLittleStorage;

// оствил тут смеха ради а так кал канеш пиздец
// идея в том что есть сервер в аппжесоне который не убрать, и есть отдельный файл
// и мне нужно было это комбинить. но терь я передумал

// Аааа зачем я всё это сделал?
public class MyStorage<T> : IHostedService
{
    private readonly string _path;
    private readonly ILogger _logger;

    private readonly List<T> _list = new();

    /// <summary>
    /// Эти предметы не удаляются и не сохраняются локально.
    /// </summary>
    private readonly List<T> _externalList = new();

    public MyStorage(string path, ILoggerFactory loggerFactory)
    {
        _path = path;
        _logger = loggerFactory.CreateLogger(this.GetType());
    }

    public T[] GetAll()
    {
        lock (_list)
        lock (_externalList)
        {
            return _list.Union(_externalList).ToArray();
        }
    }

    public void AddExternal(T item)
    {
        lock (_externalList)
            _externalList.Add(item);
    }

    public bool IsLocal(T item)
    {
        // В теории итем может быть и локал и екстернал одновременно.
        // Но мне всё равно.
        lock (_list)
            return _list.Contains(item);
    }

    public async Task RemoveAsync(T item)
    {
        bool removed;
        lock (_list)
        {
            removed = _list.Remove(item);
        }

        if (!removed)
        {
            _logger.LogWarning("Попытка удалить несуществующую запись.");
            return;
        }

        try
        {
            await SaveAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Не удалось сохранить хранилище.");
        }
    }

    private Task SaveAsync()
    {
        string fileContent;

        lock (_list)
        {
            fileContent = JsonSerializer.Serialize(_list, new JsonSerializerOptions()
            {
                WriteIndented = true
            });
        }

        return File.WriteAllTextAsync(_path, fileContent);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
            return;

        string fileContent = await File.ReadAllTextAsync(_path, cancellationToken);

        T[]? content = JsonSerializer.Deserialize<T[]>(fileContent);

        if (content == null)
            throw new Exception("Не удалось прочитать контент.");

        // На старте нет смысла это делать в локе.
        // но иде жалуется
        lock (_list)
        {
            foreach (T item in content)
                _list.Add(item);
        }

        _logger.LogInformation("Загрузили {count} записей", content.Length);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
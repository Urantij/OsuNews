using System.Text.Json;
using System.Text.Json.Serialization;

namespace OsuNews.MyLittleStorage;

// МЫ ПРОСТО ИГРАЕМ В СИМС С ТОБОЙ
// И ПРЕКРАЩАТЬ ПРИЧИН НИ ОДНОЙ НЕТ

class MyBiggerContainerConverter<T> : JsonConverter<MyBiggerContainer<T>>
    where T : class
{
    class FileWrap
    {
        public T Value { get; set; }
        public Disability? Disability { get; set; }
    }

    private readonly Func<T, object> _keyFunc;

    public MyBiggerContainerConverter(Func<T, object> keyFunc)
    {
        _keyFunc = keyFunc;
    }

    public override MyBiggerContainer<T>? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        FileWrap wrap = JsonSerializer.Deserialize<FileWrap>(reader: ref reader, options);

        object key = _keyFunc(wrap.Value);
        int hash = MyBiggerStorage<T>.CalculateHash(wrap.Value);

        return new MyBiggerContainer<T>(key, wrap.Value, hash, wrap.Disability);
    }

    public override void Write(Utf8JsonWriter writer, MyBiggerContainer<T> value, JsonSerializerOptions options)
    {
        FileWrap wrap = new()
        {
            Value = value.Value,
            Disability = value.Disability
        };

        JsonSerializer.Serialize(writer, wrap, options);
    }
}

class MyBiggerContainer<T>(object key, T value, int hash, Disability? disability)
{
    public object Key { get; } = key;
    public T Value { get; set; } = value;
    public int Hash { get; set; } = hash;

    public Disability? Disability { get; set; } = disability;

    public bool Disabled() => Disability != null;
}

class Disability(string? reason, DateTimeOffset date)
{
    public string? Reason { get; } = reason;
    public DateTimeOffset Date { get; } = date;
}

/// <summary>
/// Храниц хуйню на диске и следит за обновлениями.
/// Мега кансер потому что я скорпион по гороскопу
/// </summary>
/// <typeparam name="T"></typeparam>
public class MyBiggerStorage<T> : IHostedService
    where T : class
{
    private readonly string _path;
    private readonly Func<T, object> _keyFunc;
    private readonly ILogger _logger;

    private readonly FileSystemWatcher _watcher;

    /// <summary>
    /// Текущий таск обработки изменений.
    /// </summary>
    private TaskCompletionSource? _watcherProcessingTsc = null;

    /// <summary>
    /// Ждёт ли кто то ещё, когда текущий процессинг пройдёт.
    /// </summary>
    private bool _watcherProcessingWaits = false;

    private readonly List<MyBiggerContainer<T>> _list = [];

    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public event Action<T>? DataAdded;
    public event Action<T>? DataRemoved;
    public event Action<T, T>? DataReplaced;

    public MyBiggerStorage(string path, Func<T, object> keyFunc, ILogger logger)
    {
        _path = path;
        _keyFunc = keyFunc;
        _logger = logger;

        _jsonSerializerOptions = new JsonSerializerOptions()
        {
            WriteIndented = true,
            Converters =
            {
                new MyBiggerContainerConverter<T>(keyFunc)
            }
        };

        _watcher = new FileSystemWatcher(Path.GetDirectoryName(_path) ?? "./", Path.GetFileName(_path));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await InitContentAsync();

        _watcher.Changed += WatcherOnChanged;
        _watcher.EnableRaisingEvents = true;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _watcher.Changed -= WatcherOnChanged;
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();

        return Task.CompletedTask;
    }

    public T[] GetAll()
    {
        lock (_list)
            return _list
                .Where(l => !l.Disabled())
                .Select(l => l.Value)
                .ToArray();
    }

    public async Task DisableAsync(object key, string? reason)
    {
        DateTimeOffset date = DateTimeOffset.Now;

        lock (_list)
        {
            MyBiggerContainer<T>? container = _list.FirstOrDefault(l => l.Key == key);

            if (container == null)
            {
                _logger.LogWarning("Не удалось найти обект {key}", key);
                return;
            }

            container.Disability = new Disability(reason, date);
        }

        await SaveAsync();
    }

    private async Task InitContentAsync()
    {
        string sourceText = await File.ReadAllTextAsync(_path);

        MyBiggerContainer<T>[] data =
            JsonSerializer.Deserialize<MyBiggerContainer<T>[]>(sourceText, _jsonSerializerOptions);

        foreach (MyBiggerContainer<T> container in data)
        {
            lock (_list)
                _list.Add(container);
        }
    }

    private Task SaveAsync()
    {
        string fileContent;

        lock (_list)
        {
            fileContent = JsonSerializer.Serialize(_list, _jsonSerializerOptions);
        }

        return File.WriteAllTextAsync(_path, fileContent);
    }

    // ))))
    public static int CalculateHash(T obj)
    {
        return JsonSerializer.Serialize(obj).GetHashCode();
    }

    private void WatcherOnChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("Файл изменился, обрабатываем.");

        Task? waitTask = null;
        TaskCompletionSource? enterTcs = null;
        lock (_watcher)
        {
            if (_watcherProcessingWaits)
                return;

            if (_watcherProcessingTsc != null)
            {
                _watcherProcessingWaits = true;
                waitTask = _watcherProcessingTsc.Task;
            }
            else
            {
                _watcherProcessingTsc =
                    enterTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        Task.Run(async () =>
        {
            if (waitTask != null)
            {
                await waitTask;
                lock (_watcher)
                {
                    _watcherProcessingWaits = false;
                    _watcherProcessingTsc =
                        enterTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                }
            }

            try
            {
                await ProcessFileChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Ошибка при обработке изменений файла.");
            }

            lock (_watcher)
            {
                _watcherProcessingTsc = null;
                enterTcs.SetResult();
            }
        });
    }

    private async Task ProcessFileChangesAsync()
    {
        List<T> added = [];
        List<T> removed = [];
        List<(T older, T newer)> replaced = [];

        string sourceText = await File.ReadAllTextAsync(_path);

        MyBiggerContainer<T>[] data;
        try
        {
            data = JsonSerializer.Deserialize<MyBiggerContainer<T>[]>(sourceText, _jsonSerializerOptions);
        }
        catch (JsonException e)
        {
            _logger.LogWarning("Файл со сломанным жесоном {line}:{position}", e.LineNumber, e.BytePositionInLine);
            return;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось прочитать файл.");
            return;
        }

        foreach (MyBiggerContainer<T> diskContainer in data)
        {
            lock (_list)
            {
                int listContainerIndex = _list.FindIndex(c => c.Key == diskContainer.Key);

                if (listContainerIndex == -1)
                {
                    added.Add(diskContainer.Value);

                    _list.Add(diskContainer);
                    continue;
                }

                MyBiggerContainer<T> listContainer = _list[listContainerIndex];

                if (listContainer.Hash != diskContainer.Hash || listContainer.Disabled() != diskContainer.Disabled())
                {
                    replaced.Add((listContainer.Value, diskContainer.Value));

                    listContainer.Value = diskContainer.Value;
                    listContainer.Hash = diskContainer.Hash;
                    listContainer.Disability = diskContainer.Disability;
                    continue;
                }
            }
        }

        lock (_list)
        {
            foreach (MyBiggerContainer<T> listContainer in _list.ToArray())
            {
                if (data.Any(d => d.Value == listContainer.Value))
                    continue;

                removed.Add(listContainer.Value);

                _list.Remove(listContainer);
            }
        }

        _logger.LogInformation("Прочитали файл, +{added} -{removed} ~{replaced}", added.Count, removed.Count,
            replaced.Count);

        try
        {
            foreach (T d in added)
            {
                DataAdded?.Invoke(d);
            }

            foreach (T d in removed)
            {
                DataRemoved?.Invoke(d);
            }

            foreach ((T older, T newer) pair in replaced)
            {
                DataReplaced?.Invoke(pair.older, pair.newer);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Обработка изменений упала");
        }
    }
}
using OsuNews.Map.Models;
using OsuNews.Map.Parse;

namespace OsuNews.Map;

public class MapDownloader : IDisposable
{
    private readonly HttpClient _client;

    private readonly Uri _baseUrl = new("https://catboy.best/osu/");

    private readonly ILogger _parserLogger;

    public MapDownloader(ILoggerFactory loggerFactory)
    {
        _parserLogger = loggerFactory.CreateLogger("MapParser");

        _client = new HttpClient();
        _client.Timeout = TimeSpan.FromSeconds(20);

        _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Osu News");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="System.Net.Http.HttpRequestException">Если провалился запрос. Такое бывает.</exception>
    /// <exception cref="System.Threading.Tasks.TaskCanceledException">Такое бывает.</exception>
    /// <exception cref="BadMapException">Если карта оказалась в непонятном формате.</exception>
    public async Task<MapData> DownloadAsync(ulong id, CancellationToken cancellationToken = default)
    {
        Uri uri = new(_baseUrl, id.ToString());

        string content;
        using (HttpResponseMessage responseMessage = await _client.GetAsync(uri,
                   HttpCompletionOption.ResponseContentRead, cancellationToken: cancellationToken))
        {
            responseMessage.EnsureSuccessStatusCode();

            content = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
        }

        return MapParser.CreateFromRaw(content, logger: _parserLogger);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
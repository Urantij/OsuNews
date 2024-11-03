namespace OsuNews.Analyze;

public class MapDownloader : IDisposable
{
    private readonly HttpClient _client;

    private readonly Uri _baseUrl = new("https://catboy.best/osu/");

    public MapDownloader()
    {
        _client = new HttpClient();
        _client.Timeout = TimeSpan.FromSeconds(20);

        _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Osu News");
    }

    public async Task<Models.MapData> DownloadAsync(ulong id, CancellationToken cancellationToken = default)
    {
        Uri uri = new(_baseUrl, id.ToString());

        string content;
        using (HttpResponseMessage responseMessage = await _client.GetAsync(uri,
                   HttpCompletionOption.ResponseContentRead, cancellationToken: cancellationToken))
        {
            responseMessage.EnsureSuccessStatusCode();

            content = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
        }

        return MapParser.CreateFromRaw(content);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Options;

namespace OsuNews.MyTube;

public class TubeApi
{
    private readonly ILogger<TubeApi> _logger;
    private readonly YouTubeService _service;
    private readonly TubeConfig _config;

    public TubeApi(IOptions<TubeConfig> options, ILogger<TubeApi> logger)
    {
        _logger = logger;
        _config = options.Value;
        _service = new YouTubeService(new BaseClientService.Initializer()
        {
            ApiKey = options.Value.ApiKey,
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="System.Net.Http.HttpRequestException">Если провалился запрос. Такое бывает.</exception>
    /// <exception cref="System.Threading.Tasks.TaskCanceledException">Я не уверен на 100%, что эта ошибка падает, но мне кажется, она может.</exception>
    public async Task<string> RequestAsync()
    {
        _logger.LogDebug("Просим...");

        PlaylistItemsResource.ListRequest listRequest = _service.PlaylistItems.List("snippet");
        listRequest.PlaylistId = _config.PlaylistId;
        listRequest.MaxResults = 1;
        // listRequest.AccessToken

        PlaylistItemListResponse response = await listRequest.ExecuteAsync();

        return response.Items.First().Snippet.ResourceId.VideoId;
    }
}
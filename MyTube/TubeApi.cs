using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Options;

namespace OsuNews.MyTube;

public class TubeApi
{
    private readonly YouTubeService _service;
    private readonly TubeConfig _config;

    public TubeApi(IOptions<TubeConfig> options)
    {
        _config = options.Value;
        _service = new YouTubeService(new BaseClientService.Initializer()
        {
            ApiKey = options.Value.ApiKey, 
        });
    }
    
    public async Task<string> RequestAsync()
    {
        PlaylistItemsResource.ListRequest listRequest = _service.PlaylistItems.List("snippet");
        listRequest.PlaylistId = _config.PlaylistId;
        listRequest.MaxResults = 1;
        // listRequest.AccessToken

        PlaylistItemListResponse response = await listRequest.ExecuteAsync();

        return response.Items.First().Snippet.ResourceId.VideoId;
    }
}
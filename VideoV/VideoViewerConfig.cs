namespace OsuNews.VideoV;

public class VideoViewerConfig
{
    public static string Path => "VideoViewer";

    public string CachePath { get; set; } = "./LastVideoId";
}
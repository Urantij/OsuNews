namespace OsuNews.VideoV;

public class VideoViewerConfig
{
    public static string Path => "VideoViewer";

    public string CachePath { get; set; } = "./LastVideoId";

    // 10000 запросов в день
    // Раз в 15 сек норм (с запасом)
    public TimeSpan CheckDelay { get; set; } = TimeSpan.FromSeconds(15);
}
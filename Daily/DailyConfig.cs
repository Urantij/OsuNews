namespace OsuNews.Daily;

public class DailyConfig
{
    public static string Path => "Daily";

    public TimeSpan PassiveCheck { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan ActiveCheck { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan FailureWaitTime { get; set; } = TimeSpan.FromSeconds(20);

    public string CachePath { get; set; } = "./LastDailyCache.json";

    public bool DoAnalyze { get; set; } = true;

    public bool AttachPreview { get; set; } = false;
}
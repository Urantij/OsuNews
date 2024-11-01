namespace OsuNews.Daily;

public class DailyConfig
{
    public static string Path => "Daily";

    public TimeSpan PassiveCheck { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan ActiveCheck { get; set; } = TimeSpan.FromSeconds(30);

    public string CachePath { get; set; } = "./LastDailyCache.json";
}
namespace OsuNews.Daily;

public class DailyConfig
{
    public TimeSpan PassiveCheck { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan ActiveCheck { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan ActiveTime { get; set; } = TimeSpan.FromHours(0);

    public string SavePath { get; set; } = "./dailyinfo.json";
}
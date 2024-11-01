namespace OsuNews.Daily;

public class DailyCacheInfo(ulong id, DateTime endDate)
{
    public ulong Id { get; set; } = id;
    public DateTime EndDate { get; set; } = endDate;
}
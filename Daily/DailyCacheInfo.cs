namespace OsuNews.Daily;

/// <summary>
/// Хранит информацию о дейлике.
/// </summary>
/// <param name="id"></param>
/// <param name="endDate"></param>
public class DailyCacheInfo(ulong id, DateTime endDate)
{
    /// <summary>
    /// Айди дейлика в осу (плейлист, не мапа)
    /// </summary>
    public ulong Id { get; set; } = id;

    /// <summary>
    /// Когда плейлист дейлика закрывается
    /// </summary>
    public DateTime EndDate { get; set; } = endDate;
}
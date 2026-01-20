using OsuNews.Osu;

namespace OsuNews.Daily;

/// <summary>
/// Хранит информацию о дейлике.
/// </summary>
/// <param name="id"></param>
/// <param name="endDate"></param>
/// <param name="tags"></param>
public class DailyCacheInfo(ulong id, DateTime endDate, OsuTagData[]? tags)
{
    /// <summary>
    /// Айди дейлика в осу (плейлист, не мапа)
    /// </summary>
    public ulong Id { get; set; } = id;

    /// <summary>
    /// Когда плейлист дейлика закрывается
    /// </summary>
    public DateTime EndDate { get; set; } = endDate;

    /// <summary>
    /// Юзер теги
    /// </summary>
    public OsuTagData[]? Tags { get; set; } = tags;
}
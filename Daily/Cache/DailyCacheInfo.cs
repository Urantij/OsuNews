using OsuNews.Osu;

namespace OsuNews.Daily.Cache;

/// <summary>
/// Хранит информацию о дейлике.
/// </summary>
public class DailyCacheInfo
{
    /// <summary>
    /// Айди дейлика в осу (плейлист, не мапа)
    /// </summary>
    public ulong Id { get; set; }

    public ulong BeatmapSetId { get; set; }
    public ulong BeatmapId { get; set; }

    /// <summary>
    /// Когда плейлист дейлика закрывается
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Юзер теги
    /// </summary>
    public OsuTagData[]? Tags { get; set; }

    public DateTimeOffset? LatestTagsUpdate { get; set; }

    public DailyCacheInfo(ulong id, ulong beatmapSetId, ulong beatmapId, DateTime endDate, OsuTagData[]? tags,
        DateTimeOffset? latestTagsUpdate)
    {
        Id = id;
        BeatmapSetId = beatmapSetId;
        BeatmapId = beatmapId;
        EndDate = endDate;
        Tags = tags;
        LatestTagsUpdate = latestTagsUpdate;
    }
}
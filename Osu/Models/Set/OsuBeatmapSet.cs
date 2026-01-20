using System.Text.Json.Serialization;

namespace OsuNews.Osu.Models.Set;

public class RelatedTag
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; }
}

public class TopTagId
{
    [JsonPropertyName("tag_id")] public int TagId { get; set; }
    [JsonPropertyName("count")] public int Count { get; set; }
}

public class OsuBeatmap
{
    [JsonPropertyName("id")] public ulong Id { get; set; }
    [JsonPropertyName("top_tag_ids")] public TopTagId[] TagIds { get; set; }
}

/// <summary>
/// Мне не нужна вся информация, поэтому тут тока огрызочек.
/// </summary>
public class OsuBeatmapSet
{
    [JsonPropertyName("beatmaps")] public OsuBeatmap[] Beatmaps { get; set; }
    [JsonPropertyName("related_tags")] public RelatedTag[] RelatedTags { get; set; }
}
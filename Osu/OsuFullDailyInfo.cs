using System.Text.Json.Serialization;
using OsuNews.Map.Analyze;
using OsuNews.Osu.Models;

namespace OsuNews.Osu;

public class OsuFullDailyInfo(
    OsuGame game,
    OsuBeatmapExtended map,
    OsuTagData[]? tags,
    DateTimeOffset latestTagsUpdate,
    MapAnalyzeResult? analyze,
    bool triedToAnalyze,
    byte[]? previewContent,
    bool triedToPreview)
{
    public OsuGame Game { get; set; } = game;
    public OsuBeatmapExtended Map { get; set; } = map;
    public OsuTagData[]? Tags { get; set; } = tags;
    public DateTimeOffset LatestTagsUpdate { get; set; } = latestTagsUpdate;
    public MapAnalyzeResult? Analyze { get; set; } = analyze;
    public bool TriedToAnalyze { get; set; } = triedToAnalyze;
    [JsonIgnore] public byte[]? PreviewContent { get; set; } = previewContent;
    public bool TriedToPreview { get; set; } = triedToPreview;
}
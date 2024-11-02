using OsuNews.Analyze;
using OsuNews.Osu.Models;

namespace OsuNews.Osu;

public class OsuFullDailyInfo(OsuGame game, OsuBeatmapExtended map, MapAnalyzeResult? analyze, bool triedToAnalyze)
{
    public OsuGame Game { get; } = game;
    public OsuBeatmapExtended Map { get; } = map;
    public MapAnalyzeResult? Analyze { get; } = analyze;
    public bool TriedToAnalyze { get; } = triedToAnalyze;
}
using OsuNews.Osu.Models;

namespace OsuNews.Osu;

public class OsuApiResponse(OsuGame game, OsuBeatmapExtended map)
{
    public OsuGame Game { get; } = game;
    public OsuBeatmapExtended Map { get; } = map;
}
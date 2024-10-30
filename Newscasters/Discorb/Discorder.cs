using Discord;
using Discord.Webhook;
using Microsoft.Extensions.Options;
using OsuNews.Newscasters;
using OsuNews.Osu;
using OsuNews.Osu.Models;

namespace OsuNews.Discorb;

public class Discorder : INewscaster
{
    private readonly DiscorderConfig _config;

    private readonly DiscordWebhookClient _client;
    
    private readonly int _maxTitleLength = 256;

    public Discorder(IOptions<DiscorderConfig> options)
    {
        _config = options.Value;

        _client = new DiscordWebhookClient(_config.Hook);
    }

    public Task TellThemAsync(OsuApiResponse response)
    {
        var builder = new EmbedBuilder();

        builder.Author.WithName(response.Game.CurrentPlaylistItem.Beatmap.Beatmapset.Creator)
            .WithUrl($"https://osu.ppy.sh/users/{response.Game.CurrentPlaylistItem.Beatmap.Beatmapset.UserId}");

        builder.WithTitle($"{response.Game.CurrentPlaylistItem.Beatmap.Beatmapset.Artist} - {response.Game.CurrentPlaylistItem.Beatmap.Beatmapset.Title} ({response.Game.CurrentPlaylistItem.Beatmap.Version})"[.._maxTitleLength]);

        builder.WithDescription($"**Difficulty**: {response.Map.DifficultyRating:F1}\n**AR**: {response.Map.Ar:F1} **OD**: {response.Map.Accuracy:F!} **HP**: {response.Map.Drain:F1} **CS**: {response.Map.Cs:F1}");

        builder.WithUrl(
            $"https://osu.ppy.sh/beatmapsets/{response.Game.CurrentPlaylistItem.Beatmap.BeatmapsetId}#{response.Game.CurrentPlaylistItem.Beatmap.Mode}/{response.Game.CurrentPlaylistItem.Beatmap.Id}");
        builder.WithImageUrl(response.Game.CurrentPlaylistItem.Beatmap.Beatmapset.Covers.Cover2x);
        builder.WithFooter("помогите я застрял в холодильнике");

        return _client.SendMessageAsync(username: "Osu News", embeds: [builder.Build()]);
    }
}
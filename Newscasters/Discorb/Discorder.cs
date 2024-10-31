using System.Text;
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

    public Task TellThemAboutDailyAsync(OsuApiResponse response)
    {
        var builder = new EmbedBuilder();

        builder.Author.WithName(response.Game.CurrentPlaylistItem.Beatmap.Beatmapset.Creator)
            .WithUrl($"https://osu.ppy.sh/users/{response.Game.CurrentPlaylistItem.Beatmap.Beatmapset.UserId}");

        builder.WithTitle(
            $"{response.Game.CurrentPlaylistItem.Beatmap.Beatmapset.Artist} - {response.Game.CurrentPlaylistItem.Beatmap.Beatmapset.Title}"
                [.._maxTitleLength]);

        {
            StringBuilder sb = new();

            TimeSpan length = TimeSpan.FromSeconds(response.Map.TotalLength);

            sb.AppendLine($"**__{response.Game.CurrentPlaylistItem.Beatmap.Version}__**");

            if (response.Game.CurrentPlaylistItem.Beatmap.Mode != "osu")
            {
                sb.AppendLine($"Абалдеть, это {response.Game.CurrentPlaylistItem.Beatmap.Mode}");
            }
            
            sb.Append($"**Время:** {length.TotalMinutes}:{length.TotalSeconds:D2}");
            sb.Append($"**BPM:** {response.Map.Bpm}");
            sb.AppendLine();
            sb.AppendLine($"**Difficulty**: {response.Map.DifficultyRating:F1}");
            sb.AppendLine($"**OD**: {response.Map.Accuracy:F1}");
            sb.AppendLine($"**HP**: {response.Map.Drain:F1}");
            sb.AppendLine($"**CS**: {response.Map.Cs:F1}");

            if (response.Game.CurrentPlaylistItem.RequiredMods.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Установлены моды:");
                foreach (OsuMod mod in response.Game.CurrentPlaylistItem.RequiredMods)
                {
                    sb.AppendLine($"**{mod.Acronym}**");
                    foreach (KeyValuePair<string, object> setting in mod.Settings)
                    {
                        sb.AppendLine($"{setting.Key}: {setting.Value}");
                    }
                }
            }

            builder.WithDescription(sb.ToString());
        }

        builder.WithUrl(
            $"https://osu.ppy.sh/beatmapsets/{response.Game.CurrentPlaylistItem.Beatmap.BeatmapsetId}#{response.Game.CurrentPlaylistItem.Beatmap.Mode}/{response.Game.CurrentPlaylistItem.Beatmap.Id}");
        builder.WithImageUrl(response.Game.CurrentPlaylistItem.Beatmap.Beatmapset.Covers.Cover2x);
        builder.WithFooter("помогите я застрял в холодильнике");

        return _client.SendMessageAsync(username: "Osu News", embeds: [builder.Build()]);
    }

    public Task TellThemAboutVideoAsync(string videoId)
    {
        return _client.SendMessageAsync(username: "Osu News", text: $"Вышел новый видик!!! https://youtu.be/{videoId}");
    }
}
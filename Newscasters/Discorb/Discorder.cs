using System.Net;
using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using OsuNews.Newscasters;
using OsuNews.Osu;
using OsuNews.Osu.Models;

namespace OsuNews.Discorb;

public class Discorder : IHostedService, INewscaster
{
    private readonly ILogger<Discorder> _logger;
    private readonly DiscorderConfig _config;

    private readonly DiscordWebhookClient _client;
    private DiscordWebhook? _hook;

    private readonly int _maxTitleLength = 256;

    public Discorder(IOptions<DiscorderConfig> options, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Discorder>();
        _config = options.Value;

        if (_config.Default is null)
        {
            _logger.LogWarning("Попытка убить приложение через нул дефолт конфиг дискорда.");
            _config.Default = DiscordPostConfig.CreateDefault();
        }

        // Я крайне удивлён.
        // Я юзал Discord.Net для вебхук клиента
        // Эти придурки додумались засунуть в конструктор апи запрос
        // А я думаю, почему у меня конструктор откисает
        // И нормальный асинхронный способ они не сделали. Браво.
        // И прокси никак не засунуть. Это невероятно.

        WebProxy? proxy = null;
        if (options.Value.Proxy != null)
        {
            _logger.LogInformation("Используем прокси.");
            proxy = new WebProxy(_config.Proxy);
        }

        _client = new DiscordWebhookClient(proxy: proxy, loggerFactory: loggerFactory);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _hook = await _client.AddWebhookAsync(_config.Hook);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_hook != null)
            _client.RemoveWebhook(_hook.Id);

        return Task.CompletedTask;
    }

    public Task TellThemAboutVideoAsync(string videoId)
    {
        if (_hook == null)
        {
            _logger.LogWarning($"{nameof(TellThemAboutVideoAsync)} когда хука нет.");
            return Task.CompletedTask;
        }

        DiscordWebhookBuilder b = CreateDefaultBuilder(_config.Video)
            .WithContent($"Вышел новый видик!!! https://youtu.be/{videoId}");

        return _hook.ExecuteAsync(b);
    }

    public async Task TellThemAboutDailyAsync(OsuFullDailyInfo info)
    {
        if (_hook == null)
        {
            _logger.LogWarning($"{nameof(TellThemAboutDailyAsync)} когда хука нет.");
            return;
        }

        DiscordWebhookBuilder b = CreateDefaultBuilder(_config.Daily);

        DiscordEmbedBuilder builder = new();

        builder.WithAuthor(name: info.Game.CurrentPlaylistItem.Beatmap.Beatmapset.Creator,
            url: $"https://osu.ppy.sh/users/{info.Game.CurrentPlaylistItem.Beatmap.Beatmapset.UserId}");

        {
            string title =
                $"{info.Game.CurrentPlaylistItem.Beatmap.Beatmapset.Artist} - {info.Game.CurrentPlaylistItem.Beatmap.Beatmapset.Title}";

            if (title.Length > _maxTitleLength)
            {
                title = $"{title.Substring(0, _maxTitleLength - 3)}...";
            }

            builder.WithTitle(title);
        }

        {
            StringBuilder sb = new();

            TimeSpan length = TimeSpan.FromSeconds(info.Map.TotalLength);

            sb.AppendLine(
                $"**Сложность:** **__{info.Game.CurrentPlaylistItem.Beatmap.Version}__** ({info.Map.DifficultyRating:F1}\\*)");

            sb.Append($@"**Время:** {length:mm\:ss}");
            sb.AppendLine($" **BPM:** {info.Map.Bpm}");
            sb.AppendLine();

            if (info.Game.CurrentPlaylistItem.Beatmap.Mode != "osu")
            {
                sb.AppendLine($"Абалдеть, это {info.Game.CurrentPlaylistItem.Beatmap.Mode}");
                sb.AppendLine();
            }

            sb.Append($"**OD**: {info.Map.Accuracy:F1}");
            sb.Append($" **HP**: {info.Map.Drain:F1}");
            sb.Append($" **CS**: {info.Map.Cs:F1}");
            sb.AppendLine($" **AR**: {info.Map.Ar:F1}");

            if (info.Game.CurrentPlaylistItem.RequiredMods.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Установлены моды:");
                foreach (OsuMod mod in info.Game.CurrentPlaylistItem.RequiredMods)
                {
                    sb.AppendLine($"**{mod.Acronym}**");
                    foreach (KeyValuePair<string, object> setting in mod.Settings)
                    {
                        sb.AppendLine($"{setting.Key}: {setting.Value}");
                    }
                }
            }

            if (info.TriedToAnalyze)
            {
                sb.AppendLine();

                if (info.Analyze == null)
                {
                    sb.AppendLine("Не удалось провести анализ.");
                }
                else if (info.Analyze.IsGandon == true)
                {
                    sb.AppendLine("Внимание, автор карты гандон.");
                }
            }

            if (info.TriedToPreview && info.PreviewContent == null)
            {
                sb.AppendLine();
                sb.AppendLine("Не удалось загрузить превью.");
            }

            builder.WithDescription(sb.ToString());
        }

        builder.WithUrl(
            $"https://osu.ppy.sh/beatmapsets/{info.Game.CurrentPlaylistItem.Beatmap.BeatmapsetId}#{info.Game.CurrentPlaylistItem.Beatmap.Mode}/{info.Game.CurrentPlaylistItem.Beatmap.Id}");
        builder.WithImageUrl(info.Game.CurrentPlaylistItem.Beatmap.Beatmapset.Covers.Cover2x);
        builder.WithFooter("помогите я застрял в холодильнике");

        b.AddEmbed(builder.Build());

        MemoryStream? previewStream = null;
        if (info.TriedToPreview && info.PreviewContent != null)
        {
            previewStream = new MemoryStream(info.PreviewContent);
            // TODO возможно, стоит название вывести отдельно. Мож там быть не мп3?
            b.AddFile("preview.mp3", previewStream);
        }

        await _hook.ExecuteAsync(b);

        if (previewStream != null)
        {
            await previewStream.DisposeAsync();
        }
    }

    private DiscordWebhookBuilder CreateDefaultBuilder(DiscordPostConfig? postConfig)
    {
        return new DiscordWebhookBuilder()
            .WithUsername(postConfig?.Name ?? _config.Default.Name ?? "Osu News")
            .WithAvatarUrl(postConfig?.AvatarUrl ?? _config.Default.AvatarUrl);
    }
}
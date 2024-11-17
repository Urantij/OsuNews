using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using NetCord.Rest;
using OsuNews.Osu;
using OsuNews.Osu.Models;

namespace OsuNews.Newscasters.Discorb;

public partial class Discorder : IHostedService, INewscaster
{
    // https://discord.com/api/webhooks/1234356456/a-asd132_4sdf3-asd3234
    private static readonly Regex WebhookUriRegex = MyWebHookRegex();

    private readonly ILogger<Discorder> _logger;
    private readonly DiscorderConfig _config;

    private readonly WebhookClient _client;

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

        WebhookClientConfiguration? webhookClientConfiguration = null;
        if (proxy != null)
        {
            webhookClientConfiguration = new WebhookClientConfiguration()
            {
                Client = new RestClient(new RestClientConfiguration()
                {
                    RequestHandler = new RestRequestHandler(new HttpClientHandler()
                    {
                        Proxy = proxy
                    })
                })
            };
        }

        // Странно, что у них нет хелпера под это. А если и есть, то найти его я не смог. Документации нет.
        Match match = WebhookUriRegex.Match(_config.Hook.ToString());
        if (!match.Success)
            throw new Exception("Не удалось пропарсить хук юрл.");

        ulong hookId = ulong.Parse(match.Groups["id"].Value);
        string hookToken = match.Groups["token"].Value;

        _client = new WebhookClient(hookId, hookToken, webhookClientConfiguration);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // _hook = await _client.GetAsync(cancellationToken: cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task TellThemAboutVideoAsync(string videoId)
    {
        WebhookMessageProperties b = CreateDefaultBuilder(_config.Video)
            .WithContent($"Вышел новый видик!!! https://youtu.be/{videoId}");

        return _client.ExecuteAsync(b);
    }

    public async Task TellThemAboutDailyAsync(OsuFullDailyInfo info)
    {
        WebhookMessageProperties b = CreateDefaultBuilder(_config.Daily);

        EmbedProperties embedProperties = new();

        embedProperties.WithAuthor(new EmbedAuthorProperties()
            .WithName(info.Game.CurrentPlaylistItem.Beatmap.Beatmapset.Creator)
            .WithUrl($"https://osu.ppy.sh/users/{info.Game.CurrentPlaylistItem.Beatmap.Beatmapset.UserId}"));

        {
            string title =
                $"{info.Game.CurrentPlaylistItem.Beatmap.Beatmapset.Artist} - {info.Game.CurrentPlaylistItem.Beatmap.Beatmapset.Title}";

            if (title.Length > _maxTitleLength)
            {
                title = $"{title.Substring(0, _maxTitleLength - 3)}...";
            }

            embedProperties.WithTitle(title);
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

            embedProperties.WithDescription(sb.ToString());
        }

        embedProperties.WithUrl(
            $"https://osu.ppy.sh/beatmapsets/{info.Game.CurrentPlaylistItem.Beatmap.BeatmapsetId}#{info.Game.CurrentPlaylistItem.Beatmap.Mode}/{info.Game.CurrentPlaylistItem.Beatmap.Id}");
        embedProperties.WithImage(
            new EmbedImageProperties(info.Game.CurrentPlaylistItem.Beatmap.Beatmapset.Covers.Cover2x));
        embedProperties.WithFooter(new EmbedFooterProperties().WithText("помогите я застрял в холодильнике"));

        b.AddEmbeds(embedProperties);

        MemoryStream? previewStream = null;
        if (info.TriedToPreview && info.PreviewContent != null)
        {
            previewStream = new MemoryStream(info.PreviewContent);
            // TODO возможно, стоит название вывести отдельно. Мож там быть не мп3?
            b.AddAttachments(new AttachmentProperties("preview.mp3", previewStream));
        }

        await _client.ExecuteAsync(b);

        // TODO Чето я сомневаюсь, что дискордер должен его диспоузить
        if (previewStream != null)
        {
            await previewStream.DisposeAsync();
        }
    }

    private WebhookMessageProperties CreateDefaultBuilder(DiscordPostConfig? postConfig)
    {
        return new WebhookMessageProperties()
            .WithUsername(postConfig?.Name ?? _config.Default.Name ?? "Osu News")
            .WithAvatarUrl(postConfig?.AvatarUrl ?? _config.Default.AvatarUrl);
    }

    [GeneratedRegex(@"https://discord.com/api/webhooks/(?<id>\d+)/(?<token>.+)$", RegexOptions.Compiled)]
    private static partial Regex MyWebHookRegex();
}
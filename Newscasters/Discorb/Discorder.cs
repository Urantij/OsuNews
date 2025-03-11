using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using NetCord.Rest;
using OsuNews.MyLittleStorage;
using OsuNews.Osu;
using OsuNews.Osu.Models;
using OsuNews.Resources;

namespace OsuNews.Newscasters.Discorb;

public class DiscordStorage : MyStorage<DiscordHookConfig>
{
    public DiscordStorage(string path, ILoggerFactory loggerFactory) : base(path, loggerFactory)
    {
    }
}

public partial class Discorder : IHostedService, INewscaster
{
    class Hook(WebhookClient client, DiscordHookConfig config, CultureInfo cultureInfo)
    {
        public WebhookClient Client { get; } = client;
        public DiscordHookConfig Config { get; } = config;
        public CultureInfo CultureInfo { get; } = cultureInfo;
    }

    // https://discord.com/api/webhooks/1234356456/a-asd132_4sdf3-asd3234
    private static readonly Regex WebhookUriRegex = MyWebHookRegex();

    private readonly DiscordStorage _discordStorage;
    private readonly ILogger<Discorder> _logger;
    private readonly DiscorderConfig _config;

    private readonly List<Hook> _hooks = new();

    private readonly int _maxTitleLength = 256;

    private readonly CultureInfo _defaultCultureInfo = new("ru-RU", false);

    public Discorder(DiscordStorage discordStorage, IOptions<DiscorderConfig> options, ILoggerFactory loggerFactory)
    {
        _discordStorage = discordStorage;
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
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        WebProxy? proxy = null;
        if (_config.Proxy != null)
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

        DiscordHookConfig[] targets = _discordStorage.GetAll();
        foreach (DiscordHookConfig target in targets)
        {
            // Странно, что у них нет хелпера под это. А если и есть, то найти его я не смог. Документации нет.
            Match match = WebhookUriRegex.Match(target.Uri.ToString());
            if (!match.Success)
            {
                _logger.LogWarning("Не удалось пропарсить хук юрл. {Note}", target.Note ?? target.Uri.ToString());
                continue;
            }

            ulong hookId = ulong.Parse(match.Groups["id"].Value);
            string hookToken = match.Groups["token"].Value;

            CultureInfo? cultureInfo = null;
            if (target.Language != null)
            {
                try
                {
                    cultureInfo = CultureInfo.GetCultureInfo(target.Language);
                }
                catch (Exception e)
                {
                    _logger.LogWarning("Не удалось пропарсить язык. {Note}", target.Note ?? hookId.ToString());
                }
            }

            cultureInfo ??= _defaultCultureInfo;

            WebhookClient client = new(hookId, hookToken, webhookClientConfiguration);
            lock (_hooks)
            {
                _hooks.Add(new Hook(client, target, cultureInfo));
            }
        }

        int count;
        lock (_hooks)
            count = _hooks.Count;

        _logger.LogInformation("Добавлено {count} хуков.", count);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task TellThemAboutVideoAsync(string videoId)
    {
        Hook[] hooks;
        lock (_hooks)
            hooks = _hooks.ToArray();

        foreach (Hook hook in hooks)
        {
            DiscordPostConfig postConfig = hook.Config.Video ?? _config.Video ?? _config.Default;

            if (!postConfig.Post)
                continue;

            WebhookMessageProperties b = CreateDefaultBuilder(postConfig)
                .WithContent(
                    $"{Lines.ResourceManager.GetString("NewVideoPostTitle", hook.CultureInfo)} https://youtu.be/{videoId}");

            await SendAsync(hook, b);
        }
    }

    public async Task TellThemAboutDailyAsync(OsuFullDailyInfo info)
    {
        Hook[] hooks;
        lock (_hooks)
            hooks = _hooks.ToArray();

        foreach (Hook hook in hooks)
        {
            DiscordPostConfig postConfig = hook.Config.Daily ?? _config.Daily ?? _config.Default;

            if (!postConfig.Post)
                continue;

            WebhookMessageProperties b = FormDailyMessage(info, postConfig, hook);

            await SendAsync(hook, b);
        }
    }

    private WebhookMessageProperties FormDailyMessage(OsuFullDailyInfo info, DiscordPostConfig config, Hook hook)
    {
        WebhookMessageProperties b = CreateDefaultBuilder(config);

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
                $"**{Lines.ResourceManager.GetString("Difficulty", hook.CultureInfo)}:** **__{info.Game.CurrentPlaylistItem.Beatmap.Version}__** ({info.Map.DifficultyRating:F1}\\*)");

            sb.Append($@"**{Lines.ResourceManager.GetString("Length", hook.CultureInfo)}:** {length:mm\:ss}");
            sb.AppendLine($" **BPM:** {info.Map.Bpm}");
            sb.AppendLine();

            if (info.Game.CurrentPlaylistItem.Beatmap.Mode != "osu")
            {
                sb.AppendLine(
                    $"{Lines.ResourceManager.GetString("NonStdMod", hook.CultureInfo)} {info.Game.CurrentPlaylistItem.Beatmap.Mode}");
                sb.AppendLine();
            }

            sb.Append($"**OD**: {info.Map.Accuracy:F1}");
            sb.Append($" **HP**: {info.Map.Drain:F1}");
            sb.Append($" **CS**: {info.Map.Cs:F1}");
            sb.AppendLine($" **AR**: {info.Map.Ar:F1}");

            if (info.Game.CurrentPlaylistItem.RequiredMods.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"{Lines.ResourceManager.GetString("ForcedMods", hook.CultureInfo)}:");
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
                if (info.Analyze == null)
                {
                    sb.AppendLine();
                    sb.AppendLine($"{Lines.ResourceManager.GetString("CouldntAnalyze", hook.CultureInfo)}.");
                }
                else if (info.Analyze.IsGandon == true)
                {
                    sb.AppendLine();
                    sb.AppendLine($"{Lines.ResourceManager.GetString("GandonSpotted", hook.CultureInfo)}.");
                }
            }

            if (info.TriedToPreview && info.PreviewContent == null)
            {
                sb.AppendLine();
                sb.AppendLine($"{Lines.ResourceManager.GetString("UnableToDownloadPreview", hook.CultureInfo)}.");
            }

            embedProperties.WithDescription(sb.ToString());
        }

        embedProperties.WithUrl(
            $"https://osu.ppy.sh/beatmapsets/{info.Game.CurrentPlaylistItem.Beatmap.BeatmapsetId}#{info.Game.CurrentPlaylistItem.Beatmap.Mode}/{info.Game.CurrentPlaylistItem.Beatmap.Id}");
        embedProperties.WithImage(
            new EmbedImageProperties(info.Game.CurrentPlaylistItem.Beatmap.Beatmapset.Covers.Cover2x));
        embedProperties.WithFooter(
            new EmbedFooterProperties().WithText(
                $"{Lines.ResourceManager.GetString("MapUploaded", hook.CultureInfo)} {info.Map.Beatmapset.SubmittedDate.ToString("dd MMMM yyyy", hook.CultureInfo)}"));

        b.AddEmbeds(embedProperties);

        if (info.TriedToPreview && info.PreviewContent != null)
        {
            MemoryStream previewStream = new(info.PreviewContent);
            // TODO возможно, стоит название вывести отдельно. Мож там быть не мп3?
            b.AddAttachments(new AttachmentProperties("preview.mp3", previewStream));
        }

        return b;
    }

    private async Task SendAsync(Hook hook, WebhookMessageProperties b)
    {
        try
        {
            await hook.Client.ExecuteAsync(b);
        }
        catch (RestException re) when (re.StatusCode == HttpStatusCode.Unauthorized)
        {
            await DeleteClientAsync(hook, "имеет плохой токен");
        }
        catch (RestException re) when (re.StatusCode == HttpStatusCode.NotFound)
        {
            await DeleteClientAsync(hook, "не существует");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Не удалось отправить из-за непонятной ошибки");
        }
    }

    private Task DeleteClientAsync(Hook hook, string reason)
    {
        if (_discordStorage.IsLocal(hook.Config))
        {
            _logger.LogWarning("Вебхук {id} {reason}. Ликвидирован.", hook.Config.Note ?? hook.Client.Id.ToString(),
                reason);

            return _discordStorage.RemoveAsync(hook.Config);
        }

        _logger.LogError("Вебхук {id} {reason}. Но он не локальный..", hook.Config.Note ?? hook.Client.Id.ToString(),
            reason);

        return Task.CompletedTask;
    }

    private WebhookMessageProperties CreateDefaultBuilder(DiscordPostConfig postConfig)
    {
        return new WebhookMessageProperties()
            .WithUsername(postConfig.Name ?? "Osu News")
            .WithAvatarUrl(postConfig.AvatarUrl);
    }

    [GeneratedRegex(@"https://discord.com/api/webhooks/(?<id>\d+)/(?<token>.+)$", RegexOptions.Compiled)]
    private static partial Regex MyWebHookRegex();
}
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

public class DiscordStorage : MyBiggerStorage<DiscordHookConfig>
{
    public DiscordStorage(string path, ILogger<DiscordStorage> logger) : base(path, h => h.Uri, logger)
    {
    }
}

public partial class Discorder : IHostedService, INewscaster
{
    class Hook(WebhookClient client, DiscordHookConfig config, CultureInfo cultureInfo)
    {
        public WebhookClient Client { get; } = client;
        public DiscordHookConfig Config { get; private set; } = config;
        public CultureInfo CultureInfo { get; } = cultureInfo;

        public void ReplaceConfig(DiscordHookConfig config)
        {
            Config = config;
        }
    }

    // https://discord.com/api/webhooks/1234356456/a-asd132_4sdf3-asd3234
    private static readonly Regex WebhookUriRegex = MyWebHookRegex();

    private const string CacheDirName = "DiscordTagsHome.json";
    private readonly MyManySmallStorage<DiscordHookCache> _manySmall;

    private readonly DiscordStorage _discordStorage;
    private readonly ILogger<Discorder> _logger;
    private readonly DiscorderConfig _config;

    private readonly List<Hook> _hooks = new();

    private readonly int _maxTitleLength = 256;

    private readonly CultureInfo _defaultCultureInfo = new("ru-RU", false);

    private readonly DiscordPostConfig _defaultPostConfig = DiscordPostConfig.CreateDefault();

    public Discorder(DiscordStorage discordStorage, IOptions<AppConfig> appOptions, IOptions<DiscorderConfig> options,
        ILoggerFactory loggerFactory)
    {
        _discordStorage = discordStorage;
        _logger = loggerFactory.CreateLogger<Discorder>();
        _config = options.Value;

        _manySmall = new MyManySmallStorage<DiscordHookCache>(Path.Combine(appOptions.Value.DataPath, CacheDirName));

        // Я крайне удивлён.
        // Я юзал Discord.Net для вебхук клиента
        // Эти придурки додумались засунуть в конструктор апи запрос
        // А я думаю, почему у меня конструктор откисает
        // И нормальный асинхронный способ они не сделали. Браво.
        // И прокси никак не засунуть. Это невероятно.
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_config.Proxy != null)
        {
            _logger.LogInformation("Используем прокси.");
        }

        DiscordHookConfig[] targets = _discordStorage.GetAll();
        foreach (DiscordHookConfig target in targets)
        {
            Hook? hook = CreateHook(target);
            if (hook == null)
                continue;

            lock (_hooks)
            {
                _hooks.Add(hook);
            }
        }

        int count;
        lock (_hooks)
            count = _hooks.Count;

        _logger.LogInformation("Добавлено {count} хуков.", count);

        _manySmall.Start(cancellationToken);

        _discordStorage.DataAdded += DiscordStorageOnDataAdded;
        _discordStorage.DataRemoved += DiscordStorageOnDataRemoved;
        _discordStorage.DataReplaced += DiscordStorageOnDataReplaced;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _discordStorage.DataAdded -= DiscordStorageOnDataAdded;
        _discordStorage.DataRemoved -= DiscordStorageOnDataRemoved;
        _discordStorage.DataReplaced -= DiscordStorageOnDataReplaced;

        return Task.CompletedTask;
    }

    public async Task TellThemAboutUpdatedDailyAsync(OsuFullDailyInfo info)
    {
        Hook[] hooks;
        lock (_hooks)
            hooks = _hooks.ToArray();

        foreach (Hook hook in hooks)
        {
            bool post = GetFirstDaily(hook, config => config.Post);

            if (!post)
                continue;

            string cacheFileName = CreateCacheFileName(hook.Client.Id, info.Game.Id);

            DiscordHookCache? cache = await _manySmall.ReadAsync(cacheFileName);

            if (cache == null)
                continue;

            string? tagsLine = CreateTagsLine(info, hook);

            if (tagsLine == null || cache.Tags == tagsLine)
                continue;

            WebhookMessageProperties b = FormDailyMessage(info, hook, tagsLine);

            RestMessage? message = await UpdateAsync(cache.MessageId, hook, b);
            if (message == null)
                continue;

            await _manySmall.WriteAsync(cacheFileName, new DiscordHookCache(tagsLine, message.Id));
        }
    }

    public async Task TellThemAboutVideoAsync(string videoId)
    {
        Hook[] hooks;
        lock (_hooks)
            hooks = _hooks.ToArray();

        foreach (Hook hook in hooks)
        {
            bool post = GetFirstVideo(hook, config => config.Post);

            if (!post)
                continue;

            WebhookMessageProperties b = CreateDefaultBuilder(GetVideoConfigs(hook))
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
            bool post = GetFirstDaily(hook, config => config.Post);

            if (!post)
                continue;

            string? tagsLine = CreateTagsLine(info, hook);

            WebhookMessageProperties b = FormDailyMessage(info, hook, tagsLine);

            RestMessage? message = await SendAsync(hook, b);
            if (message == null)
                continue;

            string cacheFileName = CreateCacheFileName(hook.Client.Id, info.Game.Id);

            await _manySmall.WriteAsync(cacheFileName,
                new DiscordHookCache(tagsLine, message.Id));
        }
    }

    private void DiscordStorageOnDataAdded(DiscordHookConfig obj)
    {
        Hook? hook = CreateHook(obj);
        if (hook == null)
            return;

        lock (_hooks)
        {
            _hooks.Add(hook);
        }
    }

    private void DiscordStorageOnDataRemoved(DiscordHookConfig obj)
    {
        Hook? hook;

        lock (_hooks)
        {
            hook = _hooks.Find(h => h.Config.Uri == obj.Uri);

            if (hook == null)
            {
                _logger.LogWarning("Не удалось убрать хук {id}", obj.Note ?? obj.Uri.ToString());
                return;
            }

            _hooks.Remove(hook);
        }

        hook.Client.Dispose();
    }

    private void DiscordStorageOnDataReplaced(DiscordHookConfig arg1, DiscordHookConfig arg2)
    {
        lock (_hooks)
        {
            Hook? hook = _hooks.Find(h => h.Config.Uri == arg1.Uri);

            if (hook == null)
            {
                _logger.LogWarning("Не удалось заменить хук {id}", arg1.Note ?? arg1.Uri.ToString());
                return;
            }

            hook.ReplaceConfig(arg2);
        }
    }

    private Hook? CreateHook(DiscordHookConfig target)
    {
        // Странно, что у них нет хелпера под это. А если и есть, то найти его я не смог. Документации нет.
        Match match = WebhookUriRegex.Match(target.Uri.ToString());
        if (!match.Success)
        {
            _logger.LogWarning("Не удалось пропарсить хук юрл. {Note}", target.Note ?? target.Uri.ToString());
            return null;
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
            catch (Exception)
            {
                _logger.LogWarning("Не удалось пропарсить язык. {Note}", target.Note ?? hookId.ToString());
            }
        }

        cultureInfo ??= _defaultCultureInfo;

        // раньше делал один на всех, но при дисоузе клиента он хендлер диспоузит всегда тоже, уви.
        // а вот хттп клиент умеет не диспуозить реквестер если надо
        WebhookClientConfiguration? webhookClientConfiguration = null;
        if (_config.Proxy != null)
        {
            WebProxy proxy = new(_config.Proxy);

            webhookClientConfiguration = new WebhookClientConfiguration()
            {
                Client = new RestClient(new RestClientConfiguration()
                {
                    RequestHandler = new RestRequestHandler(new HttpClientHandler()
                    {
                        Proxy = proxy,
                    })
                })
            };
        }

        WebhookClient client = new(hookId, hookToken, webhookClientConfiguration);

        return new Hook(client, target, cultureInfo);
    }

    private WebhookMessageProperties FormDailyMessage(OsuFullDailyInfo info, Hook hook, string? tagsLine)
    {
        WebhookMessageProperties b = CreateDefaultBuilder(GetDailyConfigs(hook));

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

            // у меня была мысль, что разные челы могут хотеть разный трешхолд, но мне стало впадлу и в целом похуй
            if (tagsLine != null)
            {
                sb.AppendLine(tagsLine);
                sb.AppendLine();
            }

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

    private async Task<RestMessage?> SendAsync(Hook hook, WebhookMessageProperties b)
    {
        try
        {
            return await hook.Client.ExecuteAsync(b);
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

        return null;
    }

    private async Task<RestMessage?> UpdateAsync(ulong messageId, Hook hook, WebhookMessageProperties b)
    {
        try
        {
            return await hook.Client.ModifyMessageAsync(messageId, options =>
            {
                options.WithAttachments(b.Attachments);
                options.WithContent(b.Content);
                options.WithEmbeds(b.Embeds);
            });
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

        return null;
    }

    private Task DeleteClientAsync(Hook hook, string reason)
    {
        _logger.LogWarning("Вебхук {id} {reason}. Ликвидирован.", hook.Config.Note ?? hook.Config.Uri.ToString(),
            reason);

        lock (_hooks)
        {
            _hooks.Remove(hook);
        }

        hook.Client.Dispose();

        return _discordStorage.DisableAsync(hook.Config.Uri, reason);
    }

    private string? CreateTagsLine(OsuFullDailyInfo info, Hook hook)
    {
        // у меня была мысль, что разные челы могут хотеть разный трешхолд, но мне стало впадлу и в целом похуй
        if (info.Tags?.Length > 0)
        {
            OsuTagData[] realTags = info.Tags.Where(tag => tag.Count >= OsuApi.TagCountsToCount).ToArray();
            if (realTags.Length > 0)
            {
                return string.Join(' ', realTags.Select(t => t.Name));
            }
        }

        return null;
    }

    private WebhookMessageProperties CreateDefaultBuilder(params DiscordPostConfig?[] postConfigs)
    {
        string? name = GetFirst(config => config.Name, postConfigs);
        string? avatarUrl = GetFirst(config => config.AvatarUrl, postConfigs);

        return new WebhookMessageProperties()
            .WithUsername(name ?? "Osu News")
            .WithAvatarUrl(avatarUrl);
    }

    private T? GetFirstDaily<T>(Hook hook, Func<DiscordPostConfig, T> extract)
    {
        return GetFirst(extract, GetDailyConfigs(hook));
    }

    private T? GetFirstVideo<T>(Hook hook, Func<DiscordPostConfig, T> extract)
    {
        return GetFirst(extract, GetVideoConfigs(hook));
    }

    private DiscordPostConfig?[] GetDailyConfigs(Hook hook)
    {
        return
        [
            hook.Config.Daily, hook.Config.Default, _config.Daily, _config.Default,
            _defaultPostConfig
        ];
    }

    private DiscordPostConfig?[] GetVideoConfigs(Hook hook)
    {
        return
        [
            hook.Config.Video, hook.Config.Default, _config.Video, _config.Default,
            _defaultPostConfig
        ];
    }

    private static TResult? GetFirst<TTarget, TResult>(Func<TTarget, TResult?> extract, params TTarget?[] from)
    {
        foreach (TTarget? target in from)
        {
            if (target == null)
                continue;

            TResult? result = extract(target);

            if (result != null)
                return result;
        }

        return default;
    }

    private static string CreateCacheFileName(ulong webhookId, ulong dailyId)
    {
        return $"{webhookId}.{dailyId}.json";
    }

    [GeneratedRegex(@"https://discord.com/api/webhooks/(?<id>\d+)/(?<token>.+)$", RegexOptions.Compiled)]
    private static partial Regex MyWebHookRegex();
}
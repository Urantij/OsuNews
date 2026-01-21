using OsuNews.Daily;
using OsuNews.Daily.Cache;
using OsuNews.Daily.Check;
using OsuNews.Daily.TagsUpdater;
using OsuNews.Main;
using OsuNews.Map;
using OsuNews.MyTube;
using OsuNews.Newscasters;
using OsuNews.Newscasters.Discorb;
using OsuNews.Osu;
using OsuNews.VideoV;

namespace OsuNews;

public class Program
{
    public static void Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddOptions<AppConfig>()
            .Bind(builder.Configuration.GetSection(AppConfig.Path))
            .ValidateOnStart();

        builder.Services.AddSingleton<MapDownloader>();

        {
            if (builder.Configuration.GetSection(DiscorderConfig.Path).Exists())
            {
                builder.Services.AddSingleton<DiscordStorage>(serviceProvider =>
                    new DiscordStorage("./DiscordWebhooks.json",
                        serviceProvider.GetRequiredService<ILogger<DiscordStorage>>()));
                builder.Services.AddHostedService<DiscordStorage>(sp => sp.GetRequiredService<DiscordStorage>());

                builder.Services.AddOptions<DiscorderConfig>()
                    .Bind(builder.Configuration.GetSection(DiscorderConfig.Path))
                    .ValidateOnStart();

                builder.Services.AddSingleton<Discorder>();
                builder.Services.AddHostedService<Discorder>(sp => sp.GetRequiredService<Discorder>());
                builder.Services.AddSingleton<INewscaster, Discorder>(sp => sp.GetRequiredService<Discorder>());
            }
        }

        {
            if (builder.Configuration.GetSection(TubeConfig.Path).Exists())
            {
                builder.Services.AddOptions<TubeConfig>()
                    .Bind(builder.Configuration.GetSection(TubeConfig.Path))
                    .ValidateOnStart();

                builder.Services.AddSingleton<TubeApi>();

                builder.Services.AddOptions<VideoViewerConfig>()
                    .Bind(builder.Configuration.GetSection(VideoViewerConfig.Path))
                    .ValidateOnStart();

                builder.Services.AddSingleton<VideoViewer>();
                builder.Services.AddHostedService<VideoViewer>(sp => sp.GetRequiredService<VideoViewer>());
            }
        }

        {
            if (builder.Configuration.GetSection(OsuConfig.Path).Exists())
            {
                builder.Services.AddOptions<OsuConfig>()
                    .Bind(builder.Configuration.GetSection(OsuConfig.Path))
                    // data annotaions
                    .ValidateOnStart();

                builder.Services.AddSingleton<OsuApi>();

                builder.Services.AddOptions<DailyConfig>()
                    .Bind(builder.Configuration.GetSection(DailyConfig.Path))
                    .ValidateOnStart();

                builder.Services.AddSingleton<DailyCacheStore>();
                builder.Services.AddHostedService<DailyCacheStore>(sp => sp.GetRequiredService<DailyCacheStore>());
                builder.Services.AddSingleton<DailyWorker>();
                builder.Services.AddHostedService<DailyWorker>(sp => sp.GetRequiredService<DailyWorker>());
                builder.Services.AddSingleton<DailyTagUpdater>();
                builder.Services.AddHostedService<DailyTagUpdater>(sp => sp.GetRequiredService<DailyTagUpdater>());
            }
        }

        // Этой локализации не хватает пары классов из гайда. Алсо у ресурс менеджера есть автодополнение.
        // builder.Services.AddLocalization(s => s.ResourcesPath = "Resources");

        builder.Services.AddSingleton<MainWorker>();
        builder.Services.AddHostedService<MainWorker>(sp => sp.GetRequiredService<MainWorker>());

        var host = builder.Build();

        host.Run();
    }
}
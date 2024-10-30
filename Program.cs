using OsuNews.Daily;
using OsuNews.Discorb;
using OsuNews.Main;
using OsuNews.Newscasters;
using OsuNews.Osu;
using DiscordConfig = Discord.DiscordConfig;

namespace OsuNews;

public class Program
{
    public static void Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddHostedService<Worker>();
        {
            if (builder.Configuration.GetSection(DiscorderConfig.Path).Exists())
            {
                builder.Services.AddOptions<DiscordConfig>()
                    .Bind(builder.Configuration.GetSection(DiscorderConfig.Path))
                    .ValidateOnStart();
                builder.Services.AddSingleton<Discorder>();
                builder.Services.AddSingleton<INewscaster, Discorder>(sp => sp.GetRequiredService<Discorder>());
            }
        }
        
        builder.Services.AddOptions<OsuConfig>()
            .Bind(builder.Configuration.GetSection("Osu"))
            // data annotaions
            .ValidateOnStart();
        builder.Services.AddSingleton<OsuApi>();

        builder.Services.AddOptions<DailyConfig>()
            .Bind(builder.Configuration.GetSection("Daily"))
            .ValidateOnStart();
        builder.Services.AddSingleton<DailyWorker>();
        builder.Services.AddHostedService<DailyWorker>(sp => sp.GetRequiredService<DailyWorker>());

        builder.Services.AddSingleton<MainWorker>();
        builder.Services.AddHostedService<MainWorker>(sp => sp.GetRequiredService<MainWorker>());

        var host = builder.Build();
        host.Run();
    }
}
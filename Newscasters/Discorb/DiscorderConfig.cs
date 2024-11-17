namespace OsuNews.Newscasters.Discorb;

public class DiscordPostConfig
{
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }

    public static DiscordPostConfig CreateDefault()
    {
        return new DiscordPostConfig()
        {
            Name = "Osu News",
            AvatarUrl = null
        };
    }
}

public class DiscorderConfig
{
    public static string Path => "Discord";

    public required Uri Hook { get; set; }
    public string? Proxy { get; set; }

    public DiscordPostConfig Default { get; set; } = DiscordPostConfig.CreateDefault();

    public DiscordPostConfig? Daily { get; set; }
    public DiscordPostConfig? Video { get; set; }
}
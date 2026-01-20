namespace OsuNews.Newscasters.Discorb;

public class DiscordPostConfig
{
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }
    public bool Post { get; set; } = true;

    public static DiscordPostConfig CreateDefault()
    {
        return new DiscordPostConfig()
        {
            Name = "Osu News",
            AvatarUrl = null,
            Post = true,
        };
    }
}

public class DiscorderConfig
{
    public static string Path => "Discord";

    public string? Proxy { get; set; }

    public DiscordPostConfig? Default { get; set; }
    public DiscordPostConfig? Daily { get; set; }
    public DiscordPostConfig? Video { get; set; }
}
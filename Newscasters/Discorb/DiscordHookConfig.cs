namespace OsuNews.Newscasters.Discorb;

public class DiscordHookConfig
{
    public required Uri Uri { get; set; }
    public string? Note { get; set; }
    public string? Language { get; set; }
    
    public DiscordPostConfig? Daily { get; set; }
    public DiscordPostConfig? Video { get; set; }
}
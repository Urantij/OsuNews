namespace OsuNews.Discorb;

public class DiscorderConfig
{
    public static string Path => "Discord";

    public required Uri Hook { get; set; }
    public string? Proxy { get; set; }
}
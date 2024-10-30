namespace OsuNews.Discorb;

public class DiscorderConfig
{
    public static string Path { get; } = "Discord";
    
    public required string Hook { get; set; }
}
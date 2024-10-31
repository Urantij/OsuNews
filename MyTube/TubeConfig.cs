namespace OsuNews.MyTube;

public class TubeConfig
{
    public static string Path => "Youtube";

    public required string ApiKey { get; set; }
    public required string PlaylistId { get; set; }
}
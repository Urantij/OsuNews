namespace OsuNews.Osu;

public class OsuConfig
{
    public static string Path => "Osu";

    public required string ClientId { get; set; }
    public required string Secret { get; set; }

    // Как сделать нормальное обновляемое решение я не знаю
    // Раз в 7-8 наверное иду на мсдн, а там кукиш
    public required string RefreshToken { get; set; }
}
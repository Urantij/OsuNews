namespace OsuNews.Daily;

public class DailyConfig
{
    public static string Path => "Daily";

    /// <summary>
    /// Как часто опрашивать сервер на предмет дейлика, когда текущий дейлик ещё должен быть живым.
    /// </summary>
    public TimeSpan PassiveCheck { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Как часто опрашивать сервер на предмет дейлика, когда текущий дейлик уже должен истечь. 
    /// </summary>
    public TimeSpan ActiveCheck { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Как часто проверять новые теги на дейли карте
    /// </summary>
    public TimeSpan TagsRecheckTime { get; set; } = TimeSpan.FromMinutes(30);

    public TimeSpan FailureWaitTime { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Пытаться анализировать круги карты
    /// </summary>
    public bool DoAnalyze { get; set; } = true;

    /// <summary>
    /// Прикладывать мп3 превью карты
    /// </summary>
    public bool AttachPreview { get; set; } = false;
}
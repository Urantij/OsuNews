namespace OsuNews.Newscasters.Discorb;

public class DiscordHookCache
{
    public string? Tags { get; set; }
    public ulong MessageId { get; set; }

    public DiscordHookCache()
    {
    }

    public DiscordHookCache(string? tags, ulong messageId)
    {
        Tags = tags;
        MessageId = messageId;
    }
}
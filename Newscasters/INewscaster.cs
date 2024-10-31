using OsuNews.Osu;

namespace OsuNews.Newscasters;

public interface INewscaster
{
    public Task TellThemAboutDailyAsync(OsuApiResponse response);

    public Task TellThemAboutVideoAsync(string videoId);
}
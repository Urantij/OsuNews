using OsuNews.Osu;

namespace OsuNews.Newscasters;

public interface INewscaster
{
    public Task TellThemAboutDailyAsync(OsuFullDailyInfo info);

    public Task TellThemAboutUpdatedDailyAsync(OsuFullDailyInfo info);

    public Task TellThemAboutVideoAsync(string videoId);
}
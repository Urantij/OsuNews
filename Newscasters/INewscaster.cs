using OsuNews.Osu;

namespace OsuNews.Newscasters;

public interface INewscaster
{
    public Task TellThemAsync(OsuApiResponse response);
}
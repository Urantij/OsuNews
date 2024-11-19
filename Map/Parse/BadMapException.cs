using OsuNews.Map.Models;

namespace OsuNews.Map.Parse;

public class BadMapException : Exception
{
    public Dictionary<string, List<string>>? SectionsDict { get; }
    public HitObject[]? HitObjsList { get; }

    public BadMapException(Dictionary<string, List<string>>? sectionsDict, HitObject[]? hitObjsList,
        Exception innerException)
        : base("Bad map", innerException)
    {
        SectionsDict = sectionsDict;
        HitObjsList = hitObjsList;
    }
}
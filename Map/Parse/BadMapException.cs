using OsuNews.Map.Models;

namespace OsuNews.Map.Parse;

public class BadMapException : Exception
{
    public string? Version { get; }
    public Dictionary<string, List<string>>? SectionsDict { get; }
    public HitObject[]? HitObjsList { get; }

    public BadMapException(string? version, Dictionary<string, List<string>>? sectionsDict, HitObject[]? hitObjsList,
        Exception innerException)
        : base("Bad map", innerException)
    {
        Version = version;
        SectionsDict = sectionsDict;
        HitObjsList = hitObjsList;
    }
}
using System.Text.RegularExpressions;
using OsuNews.Map.Models;

namespace OsuNews.Map.Parse;

public static partial class MapParser
{
    private static readonly Regex _sectionRegex = SectionRegex();

    private static string SplitString { get; } = "\r\n";

    /// <summary>
    /// Парсит карту формата v14. Другие форматы можно найти на гитхабе в истории коммитов. Но мне слишком впадлу разбираться, учитывая, что этот файл правили много-много раз.
    /// </summary>
    /// <param name="raw">Строка контент мапы</param>
    /// <param name="logger">Сюда напишет ворнинг, если формат не v14</param>
    /// <returns></returns>
    /// <exception cref="BadMapException">Если формат текста непонятный.</exception>
    public static MapData CreateFromRaw(string raw, ILogger? logger = null)
    {
        string? versionString = null;
        Dictionary<string, List<string>>? sectionsDict = null;
        HitObject[]? hitObjsList = null;
        try
        {
            int endLineIndex = raw.IndexOf(SplitString, StringComparison.Ordinal);
            {
                versionString = raw[..endLineIndex];
                if (versionString != "osu file format v14")
                {
                    logger?.LogWarning("Неизвестный тип карты {version}", versionString);
                }
            }

            sectionsDict = ParseSections(raw);

            hitObjsList = sectionsDict["HitObjects"]
                .Select(ParseHitObject)
                .ToArray();

            return new MapData()
            {
                HitObjects = hitObjsList
            };
        }
        catch (Exception e)
        {
            throw new BadMapException(versionString, sectionsDict, hitObjsList, e);
        }
    }

    private static HitObject ParseHitObject(string line)
    {
        string[] split = line.Split(',');

        byte typeByte = byte.Parse(split[3]);
        HitObjectType type = ParseHitObjectType(typeByte);

        object objectParams;
        if (type == HitObjectType.Spinner)
        {
            objectParams = new SpinnerParams()
            {
                EndTime = int.Parse(split[5])
            };
        }
        else
        {
            objectParams = split[5];
        }

        return new HitObject()
        {
            X = int.Parse(split[0]),
            Y = int.Parse(split[1]),
            Time = int.Parse(split[2]),
            Type = typeByte,
            HitSound = int.Parse(split[4]),
            ObjectParams = objectParams
        };
    }

    private static HitObjectType ParseHitObjectType(byte type)
    {
        if ((type & 1) != 0)
            return HitObjectType.HitCircle;

        if ((type & 2) != 0)
            return HitObjectType.Slider;

        if ((type & 8) != 0)
            return HitObjectType.Spinner;

        if ((type & 128) != 0)
            return HitObjectType.HoldNote;

        return HitObjectType.Unknown;
    }

    private static Dictionary<string, List<string>> ParseSections(string raw)
    {
        Dictionary<string, List<string>> sectionsDict = new();

        // Это всё абсолютно ненужно, я просто хотел повеселиться
        List<string>? currentSection = null;
        int endLineIndex = raw.IndexOf(SplitString, StringComparison.Ordinal);
        int nextEndLineIndex = raw.IndexOf(SplitString, endLineIndex + SplitString.Length, StringComparison.Ordinal);

        while (endLineIndex != -1)
        {
            string line;
            if (nextEndLineIndex != -1)
                line = raw.Substring(endLineIndex + SplitString.Length,
                    nextEndLineIndex - endLineIndex - SplitString.Length);
            else
                line = raw.Substring(endLineIndex + 1);

            if (line is "" or "\n")
            {
                currentSection = null;
            }
            else
            {
                Match sectionMatch = _sectionRegex.Match(line);
                if (sectionMatch.Success)
                {
                    string currentSectionName = sectionMatch.Groups["name"].Value;
                    currentSection = new List<string>();

                    sectionsDict[currentSectionName] = currentSection;
                }
                else
                {
                    currentSection?.Add(line);
                }
            }

            endLineIndex = nextEndLineIndex;
            nextEndLineIndex = raw.IndexOf(SplitString, endLineIndex + SplitString.Length, StringComparison.Ordinal);
        }

        return sectionsDict;
    }

    [GeneratedRegex(@"\[(?<name>.+)\]")]
    private static partial Regex SectionRegex();
}
using System.Text.RegularExpressions;
using OsuNews.Analyze.Models;

namespace OsuNews.Analyze;

public static partial class MapParser
{
    private static readonly Regex _sectionRegex = SectionRegex();

    private static string SplitString { get; } = "\r\n";

    public static MapData CreateFromRaw(string raw)
    {
        int endLineIndex = raw.IndexOf(SplitString, StringComparison.Ordinal);
        {
            string versionString = raw[..endLineIndex];
            if (versionString != "osu file format v14")
            {
                throw new Exception($"Неизвестный тип карты {versionString}");
            }
        }

        Dictionary<string, List<string>> sectionsDict = ParseSections(raw);

        HitObject[] hitObjsList = sectionsDict["HitObjects"]
            .Select(ParseHitObject)
            .ToArray();

        return new MapData()
        {
            HitObjects = hitObjsList
        };
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
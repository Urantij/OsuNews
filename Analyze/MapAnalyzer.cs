using OsuNews.Analyze.Models;

namespace OsuNews.Analyze;

public static class MapAnalyzer
{
    public static MapAnalyzeResult Analyze(MapData data)
    {
        return new MapAnalyzeResult()
        {
            IsGandon = IsGandon(data)
        };
    }

    public static bool IsGandon(MapData data)
    {
        // Автор гандон, если в последних 10 нотах есть спиннер >=2сек длиной, после которого идут ещё не спинеры

        HitObject[] last = data.HitObjects.TakeLast(10).ToArray();
        bool isLongSpinner = false;

        foreach (HitObject hitObject in last)
        {
            if (hitObject.ObjectParams is SpinnerParams spinner)
            {
                TimeSpan duration = TimeSpan.FromMilliseconds(spinner.EndTime - hitObject.Time);
                isLongSpinner = duration >= TimeSpan.FromSeconds(2);
            }
            else if (isLongSpinner)
            {
                return true;
            }
        }

        return false;
    }
}
using System.Text.Json;

namespace OsuNews.Help;

public static class ExceptionLogging
{
    public static string GeneratePath(string dataPath)
    {
        string id = Guid.NewGuid().ToString("N");

        return Path.Combine(dataPath, $"{id}.data");
    }

    public static Task WriteExceptionDataAsync(JsonException jsonException, string filePath)
    {
        var content = jsonException.Data["ResponseContent"];

        return File.WriteAllTextAsync(filePath,
            $"{jsonException.ToString()}\n\n{content?.ToString() ?? "Контента нет."}");
    }
}
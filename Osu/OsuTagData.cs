namespace OsuNews.Osu;

public class OsuTagData(int id, string name, int count)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public int Count { get; } = count;
}
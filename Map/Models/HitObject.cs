namespace OsuNews.Map.Models;

public class HitObject
{
    /// <summary>
    /// Position in osu! pixels of the object.
    /// </summary>
    public int X { get; set; }
    /// <summary>
    /// Position in osu! pixels of the object.
    /// </summary>
    public int Y { get; set; }
    /// <summary>
    /// Time when the object is to be hit, in milliseconds from the beginning of the beatmap's audio.
    /// </summary>
    public int Time { get; set; }
    /// <summary>
    /// Bit flags indicating the type of the object. See the type section. https://osu.ppy.sh/wiki/en/Client/File_formats/osu_%28file_format%29#type
    /// </summary>
    public byte Type { get; set; }
    /// <summary>
    /// Bit flags indicating the hitsound applied to the object. See the hitsound section. https://osu.ppy.sh/wiki/en/Client/File_formats/osu_%28file_format%29#hitsounds
    /// </summary>
    public int HitSound { get; set; }
    /// <summary>
    /// Extra parameters specific to the object's type.
    /// </summary>
    public object? ObjectParams { get; set; }
}
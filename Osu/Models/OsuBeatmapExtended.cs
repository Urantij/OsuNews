using System.Text.Json.Serialization;

namespace OsuNews.Osu.Models;

public class Availability
{
    [JsonPropertyName("download_disabled")]
    public bool DownloadDisabled { get; set; }

    [JsonPropertyName("more_information")] public object? MoreInformation { get; set; }
}

public class BeatmapsetExtended
{
    [JsonPropertyName("artist")] public string Artist { get; set; }

    [JsonPropertyName("artist_unicode")] public string ArtistUnicode { get; set; }

    [JsonPropertyName("covers")] public Covers Covers { get; set; }

    [JsonPropertyName("creator")] public string Creator { get; set; }

    [JsonPropertyName("favourite_count")] public int FavouriteCount { get; set; }

    [JsonPropertyName("hype")] public object Hype { get; set; }

    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("nsfw")] public bool Nsfw { get; set; }

    [JsonPropertyName("offset")] public int Offset { get; set; }

    [JsonPropertyName("play_count")] public int PlayCount { get; set; }

    [JsonPropertyName("preview_url")] public string PreviewUrl { get; set; }

    [JsonPropertyName("source")] public string Source { get; set; }

    [JsonPropertyName("spotlight")] public bool Spotlight { get; set; }

    [JsonPropertyName("status")] public string Status { get; set; }

    [JsonPropertyName("title")] public string Title { get; set; }

    [JsonPropertyName("title_unicode")] public string TitleUnicode { get; set; }

    [JsonPropertyName("track_id")] public int? TrackId { get; set; }

    [JsonPropertyName("user_id")] public int UserId { get; set; }

    [JsonPropertyName("video")] public bool Video { get; set; }

    [JsonPropertyName("bpm")] public int Bpm { get; set; }

    [JsonPropertyName("can_be_hyped")] public bool CanBeHyped { get; set; }

    [JsonPropertyName("deleted_at")] public DateTime? DeletedAt { get; set; }

    [JsonPropertyName("discussion_enabled")]
    public bool DiscussionEnabled { get; set; }

    [JsonPropertyName("discussion_locked")]
    public bool DiscussionLocked { get; set; }

    [JsonPropertyName("is_scoreable")] public bool IsScoreable { get; set; }

    [JsonPropertyName("last_updated")] public DateTime LastUpdated { get; set; }

    [JsonPropertyName("legacy_thread_url")]
    public string LegacyThreadUrl { get; set; }

    [JsonPropertyName("nominations_summary")]
    public NominationsSummary NominationsSummary { get; set; }

    [JsonPropertyName("ranked")] public int Ranked { get; set; }

    [JsonPropertyName("ranked_date")] public DateTime RankedDate { get; set; }

    [JsonPropertyName("storyboard")] public bool Storyboard { get; set; }

    [JsonPropertyName("submitted_date")] public DateTime SubmittedDate { get; set; }

    [JsonPropertyName("tags")] public string Tags { get; set; }

    [JsonPropertyName("availability")] public Availability Availability { get; set; }

    [JsonPropertyName("ratings")] public List<int> Ratings { get; set; }
}

public class Failtimes
{
    [JsonPropertyName("fail")] public List<int> Fail { get; set; }

    [JsonPropertyName("exit")] public List<int> Exit { get; set; }
}

public class NominationsSummary
{
    [JsonPropertyName("current")] public int Current { get; set; }

    [JsonPropertyName("eligible_main_rulesets")]
    public List<string> EligibleMainRulesets { get; set; }

    [JsonPropertyName("required_meta")] public RequiredMeta RequiredMeta { get; set; }
}

public class RequiredMeta
{
    [JsonPropertyName("main_ruleset")] public int MainRuleset { get; set; }

    [JsonPropertyName("non_main_ruleset")] public int NonMainRuleset { get; set; }
}

public class OsuBeatmapExtended
{
    [JsonPropertyName("beatmapset_id")] public ulong BeatmapsetId { get; set; }

    [JsonPropertyName("difficulty_rating")]
    public double DifficultyRating { get; set; }

    [JsonPropertyName("id")] public ulong Id { get; set; }

    [JsonPropertyName("mode")] public string Mode { get; set; }

    [JsonPropertyName("status")] public string Status { get; set; }

    [JsonPropertyName("total_length")] public int TotalLength { get; set; }

    [JsonPropertyName("user_id")] public ulong UserId { get; set; }

    [JsonPropertyName("version")] public string Version { get; set; }

    [JsonPropertyName("accuracy")] public double Accuracy { get; set; }

    [JsonPropertyName("ar")] public double Ar { get; set; }

    [JsonPropertyName("bpm")] public double Bpm { get; set; }

    [JsonPropertyName("convert")] public bool Convert { get; set; }

    [JsonPropertyName("count_circles")] public int CountCircles { get; set; }

    [JsonPropertyName("count_sliders")] public int CountSliders { get; set; }

    [JsonPropertyName("count_spinners")] public int CountSpinners { get; set; }

    [JsonPropertyName("cs")] public double Cs { get; set; }

    [JsonPropertyName("deleted_at")] public DateTime? DeletedAt { get; set; }

    [JsonPropertyName("drain")] public double Drain { get; set; }

    [JsonPropertyName("hit_length")] public int HitLength { get; set; }

    [JsonPropertyName("is_scoreable")] public bool IsScoreable { get; set; }

    [JsonPropertyName("last_updated")] public DateTime LastUpdated { get; set; }

    [JsonPropertyName("mode_int")] public int ModeInt { get; set; }

    [JsonPropertyName("passcount")] public int Passcount { get; set; }

    [JsonPropertyName("playcount")] public int Playcount { get; set; }

    [JsonPropertyName("ranked")] public int Ranked { get; set; }

    [JsonPropertyName("url")] public string Url { get; set; }

    [JsonPropertyName("checksum")] public string Checksum { get; set; }

    [JsonPropertyName("beatmapset")] public BeatmapsetExtended Beatmapset { get; set; }

    [JsonPropertyName("failtimes")] public Failtimes Failtimes { get; set; }

    [JsonPropertyName("max_combo")] public int MaxCombo { get; set; }
}
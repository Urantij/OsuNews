using System.Text.Json.Serialization;

namespace OsuNews.Osu.Models;

public class OsuMod
{
    [JsonPropertyName("acronym")] public string Acronym { get; set; }

    [JsonPropertyName("settings")] public Dictionary<string, object> Settings { get; set; }
}

public class Beatmap
{
    [JsonPropertyName("beatmapset_id")] public int BeatmapsetId { get; set; }

    [JsonPropertyName("difficulty_rating")]
    public double DifficultyRating { get; set; }

    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("mode")] public string Mode { get; set; }

    [JsonPropertyName("status")] public string Status { get; set; }

    [JsonPropertyName("total_length")] public int TotalLength { get; set; }

    [JsonPropertyName("user_id")] public int UserId { get; set; }

    [JsonPropertyName("version")] public string Version { get; set; }

    [JsonPropertyName("beatmapset")] public Beatmapset Beatmapset { get; set; }
}

public class Beatmapset
{
    [JsonPropertyName("artist")] public string Artist { get; set; }

    [JsonPropertyName("artist_unicode")] public string ArtistUnicode { get; set; }

    [JsonPropertyName("covers")] public Covers Covers { get; set; }

    [JsonPropertyName("creator")] public string Creator { get; set; }

    [JsonPropertyName("favourite_count")] public int FavouriteCount { get; set; }

    // [JsonPropertyName("hype")] public object Hype { get; set; }

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
}

public class Country
{
    [JsonPropertyName("code")] public string Code { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }
}

public class Covers
{
    [JsonPropertyName("cover")] public string Cover { get; set; }

    [JsonPropertyName("cover@2x")] public string Cover2x { get; set; }

    [JsonPropertyName("card")] public string Card { get; set; }

    [JsonPropertyName("card@2x")] public string Card2x { get; set; }

    [JsonPropertyName("list")] public string List { get; set; }

    [JsonPropertyName("list@2x")] public string List2x { get; set; }

    [JsonPropertyName("slimcover")] public string Slimcover { get; set; }

    [JsonPropertyName("slimcover@2x")] public string Slimcover2x { get; set; }
}

public class PlaylistItem
{
    [JsonPropertyName("id")] public ulong Id { get; set; }

    [JsonPropertyName("room_id")] public ulong RoomId { get; set; }

    [JsonPropertyName("beatmap_id")] public ulong BeatmapId { get; set; }

    [JsonPropertyName("ruleset_id")] public int RulesetId { get; set; }

    [JsonPropertyName("allowed_mods")] public List<OsuMod> AllowedMods { get; set; }

    [JsonPropertyName("required_mods")] public List<OsuMod> RequiredMods { get; set; }

    [JsonPropertyName("expired")] public bool Expired { get; set; }

    [JsonPropertyName("owner_id")] public ulong OwnerId { get; set; }

    [JsonPropertyName("playlist_order")] public object PlaylistOrder { get; set; }

    // [JsonPropertyName("played_at")] public DateTime? PlayedAt { get; set; }

    [JsonPropertyName("beatmap")] public Beatmap Beatmap { get; set; }
}

public class DifficultyRange
{
    [JsonPropertyName("max")] public double Max { get; set; }

    [JsonPropertyName("min")] public double Min { get; set; }
}

public class Host
{
    [JsonPropertyName("avatar_url")] public string AvatarUrl { get; set; }

    [JsonPropertyName("country_code")] public string CountryCode { get; set; }

    [JsonPropertyName("default_group")] public string DefaultGroup { get; set; }

    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("is_active")] public bool IsActive { get; set; }

    [JsonPropertyName("is_bot")] public bool IsBot { get; set; }

    [JsonPropertyName("is_deleted")] public bool IsDeleted { get; set; }

    [JsonPropertyName("is_online")] public bool IsOnline { get; set; }

    [JsonPropertyName("is_supporter")] public bool IsSupporter { get; set; }

    // [JsonPropertyName("last_visit")] public DateTime LastVisit { get; set; }

    [JsonPropertyName("pm_friends_only")] public bool PmFriendsOnly { get; set; }

    [JsonPropertyName("profile_colour")] public string ProfileColour { get; set; }

    [JsonPropertyName("username")] public string Username { get; set; }

    [JsonPropertyName("country")] public Country Country { get; set; }
}

public class PlaylistItemStats
{
    [JsonPropertyName("count_active")] public int CountActive { get; set; }

    [JsonPropertyName("count_total")] public int CountTotal { get; set; }

    [JsonPropertyName("ruleset_ids")] public List<int> RulesetIds { get; set; }
}

public class Participant
{
    [JsonPropertyName("avatar_url")] public string AvatarUrl { get; set; }

    [JsonPropertyName("country_code")] public string CountryCode { get; set; }

    [JsonPropertyName("default_group")] public string DefaultGroup { get; set; }

    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("is_active")] public bool IsActive { get; set; }

    [JsonPropertyName("is_bot")] public bool IsBot { get; set; }

    [JsonPropertyName("is_deleted")] public bool IsDeleted { get; set; }

    [JsonPropertyName("is_online")] public bool IsOnline { get; set; }

    [JsonPropertyName("is_supporter")] public bool IsSupporter { get; set; }

    // [JsonPropertyName("last_visit")] public DateTime? LastVisit { get; set; }

    [JsonPropertyName("pm_friends_only")] public bool PmFriendsOnly { get; set; }

    [JsonPropertyName("profile_colour")] public string ProfileColour { get; set; }

    [JsonPropertyName("username")] public string Username { get; set; }
}

public class OsuGame
{
    [JsonPropertyName("id")] public ulong Id { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("category")] public string Category { get; set; }

    [JsonPropertyName("type")] public string Type { get; set; }

    [JsonPropertyName("user_id")] public ulong UserId { get; set; }

    [JsonPropertyName("starts_at")] public DateTimeOffset StartsAt { get; set; }

    [JsonPropertyName("ends_at")] public DateTimeOffset EndsAt { get; set; }

    [JsonPropertyName("max_attempts")] public int? MaxAttempts { get; set; }

    [JsonPropertyName("participant_count")]
    public int ParticipantCount { get; set; }

    [JsonPropertyName("channel_id")] public int ChannelId { get; set; }

    [JsonPropertyName("active")] public bool Active { get; set; }

    [JsonPropertyName("has_password")] public bool HasPassword { get; set; }

    [JsonPropertyName("queue_mode")] public string QueueMode { get; set; }

    [JsonPropertyName("auto_skip")] public bool AutoSkip { get; set; }

    [JsonPropertyName("current_playlist_item")]
    public PlaylistItem CurrentPlaylistItem { get; set; }

    [JsonPropertyName("difficulty_range")] public DifficultyRange DifficultyRange { get; set; }

    [JsonPropertyName("host")] public Host Host { get; set; }

    [JsonPropertyName("playlist_item_stats")]
    public PlaylistItemStats PlaylistItemStats { get; set; }

    [JsonPropertyName("recent_participants")]
    public List<Participant> RecentParticipants { get; set; }
}

// public class Settings
// {
//     [JsonPropertyName("speed_change")] public double? SpeedChange { get; set; }
//
//     [JsonPropertyName("final_rate")] public double? FinalRate { get; set; }
//
//     [JsonPropertyName("max_depth")] public int? MaxDepth { get; set; }
//
//     [JsonPropertyName("adjust_pitch")] public bool? AdjustPitch { get; set; }
//
//     [JsonPropertyName("only_fade_approach_circles")]
//     public bool? OnlyFadeApproachCircles { get; set; }
//
//     [JsonPropertyName("strength")] public double? Strength { get; set; }
// }
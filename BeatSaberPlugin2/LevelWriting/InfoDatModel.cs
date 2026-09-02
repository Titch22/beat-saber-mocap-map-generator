using System.Collections.Generic;
using Newtonsoft.Json;

namespace BeatSaberPlugin2.LevelWriting;

/// <summary>POCOs mirroring Beat Saber's v2 "Info.dat" custom level format (only the fields we need).</summary>
internal class InfoDat
{
    [JsonProperty("_version")]
    public string Version { get; set; } = "2.0.0";

    [JsonProperty("_songName")]
    public string SongName { get; set; } = "";

    [JsonProperty("_songSubName")]
    public string SongSubName { get; set; } = "";

    [JsonProperty("_songAuthorName")]
    public string SongAuthorName { get; set; } = "Inconnu";

    [JsonProperty("_levelAuthorName")]
    public string LevelAuthorName { get; set; } = "BeatSaberPlugin2";

    [JsonProperty("_beatsPerMinute")]
    public float BeatsPerMinute { get; set; } = 60f;

    [JsonProperty("_songTimeOffset")]
    public float SongTimeOffset { get; set; } = 0f;

    [JsonProperty("_shuffle")]
    public float Shuffle { get; set; } = 0f;

    [JsonProperty("_shufflePeriod")]
    public float ShufflePeriod { get; set; } = 0.5f;

    [JsonProperty("_previewStartTime")]
    public float PreviewStartTime { get; set; } = 10f;

    [JsonProperty("_previewDuration")]
    public float PreviewDuration { get; set; } = 10f;

    [JsonProperty("_songFilename")]
    public string SongFilename { get; set; } = "song.egg";

    [JsonProperty("_coverImageFilename")]
    public string CoverImageFilename { get; set; } = "cover.png";

    [JsonProperty("_environmentName")]
    public string EnvironmentName { get; set; } = "DefaultEnvironment";

    [JsonProperty("_allDirectionsEnvironmentName")]
    public string AllDirectionsEnvironmentName { get; set; } = "GlassDesertEnvironment";

    [JsonProperty("_difficultyBeatmapSets")]
    public List<DifficultyBeatmapSetDat> DifficultyBeatmapSets { get; set; } = new();
}

internal class DifficultyBeatmapSetDat
{
    [JsonProperty("_beatmapCharacteristicName")]
    public string BeatmapCharacteristicName { get; set; } = "Standard";

    [JsonProperty("_difficultyBeatmaps")]
    public List<DifficultyBeatmapEntryDat> DifficultyBeatmaps { get; set; } = new();
}

internal class DifficultyBeatmapEntryDat
{
    [JsonProperty("_difficulty")]
    public string Difficulty { get; set; } = "Expert";

    [JsonProperty("_difficultyRank")]
    public int DifficultyRank { get; set; } = 7;

    [JsonProperty("_beatmapFilename")]
    public string BeatmapFilename { get; set; } = "ExpertStandard.dat";

    [JsonProperty("_noteJumpMovementSpeed")]
    public float NoteJumpMovementSpeed { get; set; } = 10f;

    [JsonProperty("_noteJumpStartBeatOffset")]
    public float NoteJumpStartBeatOffset { get; set; } = 0f;
}

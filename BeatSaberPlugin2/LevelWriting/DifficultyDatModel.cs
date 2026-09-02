using System.Collections.Generic;
using Newtonsoft.Json;

namespace BeatSaberPlugin2.LevelWriting;

/// <summary>POCO mirroring Beat Saber's v2 difficulty file format (e.g. "ExpertStandard.dat").</summary>
internal class DifficultyDat
{
    [JsonProperty("_version")]
    public string Version { get; set; } = "2.0.0";

    [JsonProperty("_notes")]
    public List<NoteDat> Notes { get; set; } = new();

    [JsonProperty("_obstacles")]
    public List<object> Obstacles { get; set; } = new();

    [JsonProperty("_events")]
    public List<object> Events { get; set; } = new();
}

internal class NoteDat
{
    [JsonProperty("_time")]
    public float Time { get; set; }

    [JsonProperty("_lineIndex")]
    public int LineIndex { get; set; }

    [JsonProperty("_lineLayer")]
    public int LineLayer { get; set; }

    [JsonProperty("_type")]
    public int Type { get; set; }

    [JsonProperty("_cutDirection")]
    public int CutDirection { get; set; }
}

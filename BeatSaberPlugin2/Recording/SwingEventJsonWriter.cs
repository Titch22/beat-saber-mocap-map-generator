using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace BeatSaberPlugin2.Recording;

/// <summary>
/// Dumps detected swings to a JSON file so the detection/tuning can be inspected and iterated on
/// without needing the full map generator yet. Written under the game's <c>UserData</c> folder,
/// like most other mods' data.
/// </summary>
internal static class SwingEventJsonWriter
{
    /// <summary>Writes <paramref name="events"/> to a timestamped JSON file and returns its full path.</summary>
    public static string Write(IReadOnlyList<SwingEvent> events, string songName)
    {
        var gameRoot = Directory.GetParent(Application.dataPath)!.FullName;
        var outputDir = Path.Combine(gameRoot, "UserData", "BeatSaberPlugin2", "swings");
        Directory.CreateDirectory(outputDir);

        var fileName = $"{SanitizeFileName(songName)}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        var path = Path.Combine(outputDir, fileName);

        var payload = events.Select(e => new
        {
            time = e.Time,
            hand = e.Hand.ToString(),
            direction = e.Direction.ToString(),
            position = new { x = e.Position.x, y = e.Position.y, z = e.Position.z },
            speed = e.Speed,
        });

        File.WriteAllText(path, JsonConvert.SerializeObject(payload, Formatting.Indented));
        return path;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalidChar, '_');
        }

        return name;
    }
}

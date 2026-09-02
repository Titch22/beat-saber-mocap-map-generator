using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BeatSaberPlugin2.Audio;
using BeatSaberPlugin2.Generation;
using Newtonsoft.Json;

namespace BeatSaberPlugin2.LevelWriting;

/// <summary>
/// Writes a complete, vanilla-compatible Beat Saber v2 custom level folder (Info.dat + one
/// difficulty file + song.egg + a placeholder cover) so it can be picked up by the game's own
/// custom level scanner - no dependency on SongCore or any other mod.
///
/// This class is deliberately Unity-API-free (pure C#/file I/O) so it's safe to call from a
/// background thread - <paramref name="gameRoot"/> and <paramref name="coverPng"/> must be
/// obtained on the main thread beforehand (see <see cref="CoverImageProvider"/>), since Unity
/// APIs like Texture2D and Application.dataPath are not thread-safe and will crash the game
/// (not just throw) if touched off the main thread.
/// </summary>
internal static class CustomLevelWriter
{
    /// <summary>Writes the level and returns the folder it was written to.</summary>
    public static string Write(
        string gameRoot, string songName, PcmAudio pcm, IReadOnlyList<GeneratedNote> notes, byte[] coverPng)
    {
        var customLevelsRoot = Path.Combine(gameRoot, "Beat Saber_Data", "CustomLevels");
        var slug = $"{Sanitize(songName)}_{DateTime.Now:yyyyMMdd_HHmmss}";
        var levelFolder = Path.Combine(customLevelsRoot, slug);
        Directory.CreateDirectory(levelFolder);

        File.WriteAllBytes(Path.Combine(levelFolder, "song.egg"), OggEggEncoder.Encode(pcm));
        File.WriteAllBytes(Path.Combine(levelFolder, "cover.png"), coverPng);

        var difficultyDat = new DifficultyDat
        {
            Notes = notes.Select(n => new NoteDat
            {
                Time = n.BeatTime,
                LineIndex = n.LineIndex,
                LineLayer = n.LineLayer,
                Type = n.Type,
                CutDirection = n.CutDirection,
            }).ToList(),
        };
        File.WriteAllText(
            Path.Combine(levelFolder, "ExpertStandard.dat"),
            JsonConvert.SerializeObject(difficultyDat, Formatting.Indented));

        var infoDat = BuildInfoDat(songName);
        File.WriteAllText(
            Path.Combine(levelFolder, "Info.dat"),
            JsonConvert.SerializeObject(infoDat, Formatting.Indented));

        return levelFolder;
    }

    private static InfoDat BuildInfoDat(string songName)
    {
        return new InfoDat
        {
            SongName = songName,
            DifficultyBeatmapSets = new List<DifficultyBeatmapSetDat>
            {
                new()
                {
                    DifficultyBeatmaps = new List<DifficultyBeatmapEntryDat> { new() },
                },
            },
        };
    }

    private static string Sanitize(string name)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalidChar, '_');
        }

        return name;
    }
}

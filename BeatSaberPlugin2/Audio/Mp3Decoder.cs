using System.Collections.Generic;
using NAudio.Wave;

namespace BeatSaberPlugin2.Audio;

/// <summary>
/// Decodes an mp3 file to raw PCM samples using Windows Media Foundation (via NAudio). Unity
/// doesn't support importing mp3 at runtime, so the file is fully decoded in memory here and
/// turned into an <see cref="UnityEngine.AudioClip"/> separately via <see cref="AudioClipFactory"/>.
/// Decoding a multi-minute track can take a noticeable amount of time - call this off Unity's
/// main thread.
/// </summary>
internal static class Mp3Decoder
{
    public static PcmAudio Decode(string path)
    {
        using var reader = new MediaFoundationReader(path);
        var sampleProvider = reader.ToSampleProvider();
        var format = sampleProvider.WaveFormat;

        var samples = new List<float>();
        var buffer = new float[format.SampleRate * format.Channels]; // ~1s chunks
        int read;
        while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (var i = 0; i < read; i++)
            {
                samples.Add(buffer[i]);
            }
        }

        return new PcmAudio(samples.ToArray(), format.Channels, format.SampleRate);
    }
}

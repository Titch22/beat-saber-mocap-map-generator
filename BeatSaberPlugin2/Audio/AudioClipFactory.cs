using UnityEngine;

namespace BeatSaberPlugin2.Audio;

/// <summary>
/// Turns decoded PCM samples into a Unity <see cref="AudioClip"/>. Must run on Unity's main
/// thread - <see cref="AudioClip.Create"/> and <see cref="AudioClip.SetData(float[], int)"/> are
/// not thread-safe.
/// </summary>
internal static class AudioClipFactory
{
    public static AudioClip Create(string clipName, PcmAudio pcm)
    {
        var lengthSamples = pcm.Samples.Length / pcm.Channels;
        var clip = AudioClip.Create(clipName, lengthSamples, pcm.Channels, pcm.SampleRate, false);
        clip.SetData(pcm.Samples, 0);
        return clip;
    }
}

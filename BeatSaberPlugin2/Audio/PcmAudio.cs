namespace BeatSaberPlugin2.Audio;

/// <summary>Raw interleaved PCM samples decoded from an audio file, ready to become an <see cref="UnityEngine.AudioClip"/>.</summary>
internal sealed class PcmAudio
{
    public PcmAudio(float[] samples, int channels, int sampleRate)
    {
        Samples = samples;
        Channels = channels;
        SampleRate = sampleRate;
    }

    /// <summary>Interleaved samples (e.g. for stereo: L,R,L,R,...).</summary>
    public float[] Samples { get; }

    public int Channels { get; }

    public int SampleRate { get; }
}

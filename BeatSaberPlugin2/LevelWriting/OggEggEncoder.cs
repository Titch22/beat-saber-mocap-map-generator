using System;
using System.IO;
using BeatSaberPlugin2.Audio;
using OggVorbisEncoder;

namespace BeatSaberPlugin2.LevelWriting;

/// <summary>
/// Encodes decoded PCM audio to an Ogg Vorbis stream - Beat Saber's ".egg" song files are just
/// Ogg Vorbis files with a renamed extension. Adapted from the OggVorbisEncoder library's own
/// example (github.com/SteveLillis/.NET-Ogg-Vorbis-Encoder).
/// </summary>
internal static class OggEggEncoder
{
    private const int WriteBufferSize = 512;

    public static byte[] Encode(PcmAudio pcm)
    {
        var channelSamples = Deinterleave(pcm);

        using var outputData = new MemoryStream();

        // Stores all the static vorbis bitstream settings.
        var info = VorbisInfo.InitVariableBitRate(pcm.Channels, pcm.SampleRate, 0.5f);
        var oggStream = new OggStream(new Random().Next());

        // Vorbis streams begin with three headers: codec setup, comments, and the bitstream
        // codebook - all mandated by the Ogg/Vorbis spec.
        var comments = new Comments();
        oggStream.PacketIn(HeaderPacketBuilder.BuildInfoPacket(info));
        oggStream.PacketIn(HeaderPacketBuilder.BuildCommentsPacket(comments));
        oggStream.PacketIn(HeaderPacketBuilder.BuildBooksPacket(info));
        FlushPages(oggStream, outputData, force: true);

        var processingState = ProcessingState.Create(info);
        for (var readIndex = 0; readIndex < channelSamples[0].Length; readIndex += WriteBufferSize)
        {
            var length = Math.Min(WriteBufferSize, channelSamples[0].Length - readIndex);
            processingState.WriteData(channelSamples, length, readIndex);

            while (!oggStream.Finished && processingState.PacketOut(out var packet))
            {
                oggStream.PacketIn(packet);
                FlushPages(oggStream, outputData, force: false);
            }
        }

        processingState.WriteEndOfStream();
        while (!oggStream.Finished && processingState.PacketOut(out var packet))
        {
            oggStream.PacketIn(packet);
            FlushPages(oggStream, outputData, force: false);
        }

        FlushPages(oggStream, outputData, force: true);

        return outputData.ToArray();
    }

    private static float[][] Deinterleave(PcmAudio pcm)
    {
        var samplesPerChannel = pcm.Samples.Length / pcm.Channels;
        var channels = new float[pcm.Channels][];
        for (var ch = 0; ch < pcm.Channels; ch++)
        {
            channels[ch] = new float[samplesPerChannel];
        }

        for (var i = 0; i < samplesPerChannel; i++)
        {
            for (var ch = 0; ch < pcm.Channels; ch++)
            {
                channels[ch][i] = pcm.Samples[(i * pcm.Channels) + ch];
            }
        }

        return channels;
    }

    private static void FlushPages(OggStream oggStream, Stream output, bool force)
    {
        while (oggStream.PageOut(out var page, force))
        {
            output.Write(page.Header, 0, page.Header.Length);
            output.Write(page.Body, 0, page.Body.Length);
        }
    }
}

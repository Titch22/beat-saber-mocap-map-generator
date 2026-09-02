using UnityEngine;

namespace BeatSaberPlugin2.LevelWriting;

/// <summary>
/// Generates a plain placeholder cover image at runtime instead of embedding a binary asset in
/// the plugin - Beat Saber's Info.dat requires a cover image file to exist, but its content
/// doesn't need to be anything fancy for the MVP.
/// </summary>
internal static class CoverImageProvider
{
    public static byte[] GeneratePlaceholderPng()
    {
        const int size = 256;
        var texture = new Texture2D(size, size, TextureFormat.RGB24, mipChain: false);

        var color = new Color(0.25f, 0.55f, 0.85f);
        var pixels = new Color[size * size];
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        texture.SetPixels(pixels);
        texture.Apply();

        var png = texture.EncodeToPNG();
        Object.Destroy(texture);
        return png;
    }
}

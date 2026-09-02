using UnityEngine;

namespace BeatSaberPlugin2.Recording;

/// <summary>A single timestamped snapshot of both hand poses, captured during recording.</summary>
internal readonly struct PoseSample
{
    public PoseSample(float time, Vector3 leftPosition, Quaternion leftRotation, Vector3 rightPosition, Quaternion rightRotation)
    {
        Time = time;
        LeftPosition = leftPosition;
        LeftRotation = leftRotation;
        RightPosition = rightPosition;
        RightRotation = rightRotation;
    }

    /// <summary>Seconds since the song started playing (<c>AudioSource.time</c>), not <c>Time.time</c> - avoids drift.</summary>
    public float Time { get; }

    public Vector3 LeftPosition { get; }

    public Quaternion LeftRotation { get; }

    public Vector3 RightPosition { get; }

    public Quaternion RightRotation { get; }
}

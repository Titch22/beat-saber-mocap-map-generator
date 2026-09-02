using UnityEngine;

namespace BeatSaberPlugin2.Recording;

/// <summary>A single detected swing: one hand moving fast enough, at one point in time, in one direction.</summary>
internal readonly struct SwingEvent
{
    public SwingEvent(float time, Hand hand, SwingDirection direction, Vector3 position, float speed)
    {
        Time = time;
        Hand = hand;
        Direction = direction;
        Position = position;
        Speed = speed;
    }

    /// <summary>Seconds since the song started playing (matches <see cref="PoseSample.Time"/>).</summary>
    public float Time { get; }

    public Hand Hand { get; }

    public SwingDirection Direction { get; }

    /// <summary>Hand position at the moment of the swing, for later mapping to a grid column/row.</summary>
    public Vector3 Position { get; }

    /// <summary>Peak speed of the swing in m/s, kept around for tuning/debugging.</summary>
    public float Speed { get; }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeatSaberPlugin2.Recording;

/// <summary>
/// Turns a raw pose timeline into a list of discrete swings: local speed peaks above a
/// threshold, at least <see cref="MinIntervalSeconds"/> apart per hand, classified into one of
/// 8 compass directions from the hand's velocity at the peak.
/// </summary>
internal static class SwingDetector
{
    private const float MinSwingSpeedMetersPerSecond = 1.5f;
    private const float MinIntervalSeconds = 0.15f;

    /// <summary>
    /// A second speed peak in the *same* classified direction within this window of the last
    /// accepted one is treated as noise/a wobble mid-swing rather than a separate swing (e.g. the
    /// hand very briefly decelerating then re-accelerating before actually reversing direction).
    /// Wider than <see cref="MinIntervalSeconds"/> since it only applies to same-direction
    /// repeats - alternating swings (up then down, etc.) can legitimately be faster than this.
    /// </summary>
    private const float SameDirectionMergeWindowSeconds = 0.35f;

    public static List<SwingEvent> Detect(IReadOnlyList<PoseSample> samples)
    {
        var events = new List<SwingEvent>();
        events.AddRange(DetectForHand(samples, Hand.Left, s => s.LeftPosition));
        events.AddRange(DetectForHand(samples, Hand.Right, s => s.RightPosition));
        events.Sort((a, b) => a.Time.CompareTo(b.Time));
        return events;
    }

    private static List<SwingEvent> DetectForHand(
        IReadOnlyList<PoseSample> samples, Hand hand, Func<PoseSample, Vector3> position)
    {
        var result = new List<SwingEvent>();
        if (samples.Count < 3)
        {
            return result;
        }

        // Precompute per-sample velocity/speed once instead of recomputing neighbours repeatedly.
        var velocities = new Vector3[samples.Count];
        var speeds = new float[samples.Count];
        for (var i = 1; i < samples.Count; i++)
        {
            var dt = samples[i].Time - samples[i - 1].Time;
            if (dt <= 0f)
            {
                continue;
            }

            velocities[i] = (position(samples[i]) - position(samples[i - 1])) / dt;
            speeds[i] = velocities[i].magnitude;
        }

        var lastSwingTime = float.NegativeInfinity;
        var lastSwingDirection = SwingDirection.Any;
        for (var i = 1; i < samples.Count - 1; i++)
        {
            if (speeds[i] < MinSwingSpeedMetersPerSecond)
            {
                continue;
            }

            // Only keep local maxima - the exact frame the hand was moving fastest.
            if (speeds[i] < speeds[i - 1] || speeds[i] < speeds[i + 1])
            {
                continue;
            }

            var timeSinceLastSwing = samples[i].Time - lastSwingTime;
            if (timeSinceLastSwing < MinIntervalSeconds)
            {
                continue;
            }

            var direction = ClassifyDirection(velocities[i]);
            if (direction == lastSwingDirection && timeSinceLastSwing < SameDirectionMergeWindowSeconds)
            {
                // Same direction, too soon after the last one - almost certainly a wobble in the
                // middle of the same physical swing rather than a second, distinct one.
                continue;
            }

            result.Add(new SwingEvent(samples[i].Time, hand, direction, position(samples[i]), speeds[i]));
            lastSwingTime = samples[i].Time;
            lastSwingDirection = direction;
        }

        return result;
    }

    /// <summary>
    /// Snaps a velocity vector to the nearest of Beat Saber's 8 compass cut directions, looking
    /// only at the vertical/horizontal (up-down/left-right) plane - the swing's depth (forward/
    /// back) component is ignored since it doesn't correspond to a cuttable direction.
    /// </summary>
    private static SwingDirection ClassifyDirection(Vector3 velocity)
    {
        var x = velocity.x;
        var y = velocity.y;
        if (Mathf.Abs(x) < 0.01f && Mathf.Abs(y) < 0.01f)
        {
            return SwingDirection.Any;
        }

        var angle = Mathf.Atan2(y, x) * Mathf.Rad2Deg; // 0 = right, 90 = up, 180/-180 = left

        var best = SwingDirection.Any;
        var bestDelta = float.MaxValue;
        foreach (var (compassAngle, direction) in CompassDirections)
        {
            var delta = Mathf.Abs(Mathf.DeltaAngle(angle, compassAngle));
            if (delta < bestDelta)
            {
                bestDelta = delta;
                best = direction;
            }
        }

        return best;
    }

    private static readonly (float Angle, SwingDirection Direction)[] CompassDirections =
    {
        (0f, SwingDirection.Right),
        (45f, SwingDirection.UpRight),
        (90f, SwingDirection.Up),
        (135f, SwingDirection.UpLeft),
        (180f, SwingDirection.Left),
        (-135f, SwingDirection.DownLeft),
        (-90f, SwingDirection.Down),
        (-45f, SwingDirection.DownRight),
    };
}

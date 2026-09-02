using System.Collections.Generic;
using BeatSaberPlugin2.Recording;
using UnityEngine;

namespace BeatSaberPlugin2.Generation;

/// <summary>
/// Converts detected swings into placed notes. For the MVP this just needs to be "playable
/// enough" - each swing already respects a minimum per-hand spacing (from <see cref="SwingDetector"/>),
/// so this only needs to pick a believable grid position and reuse the detected cut direction.
/// </summary>
internal static class MapGenerator
{
    public static List<GeneratedNote> Generate(IReadOnlyList<SwingEvent> events)
    {
        var notes = new List<GeneratedNote>(events.Count);
        if (events.Count == 0)
        {
            return notes;
        }

        var leftRange = ComputeRange(events, Hand.Left);
        var rightRange = ComputeRange(events, Hand.Right);

        foreach (var swing in events)
        {
            var range = swing.Hand == Hand.Left ? leftRange : rightRange;
            var column = QuantizeColumn(swing.Position, range);
            var lineLayer = QuantizeLayer(swing.Position, range);
            var type = swing.Hand == Hand.Left ? 0 : 1;
            var columnBase = swing.Hand == Hand.Left ? 0 : 2;

            // SwingDirection's enum order matches Beat Saber's _cutDirection values exactly
            // (Up=0, Down=1, Left=2, Right=3, UpLeft=4, UpRight=5, DownLeft=6, DownRight=7, Any=8).
            var cutDirection = (int)swing.Direction;

            notes.Add(new GeneratedNote(swing.Time, columnBase + column, lineLayer, type, cutDirection));

            // A swing that's unusually fast/wide for this hand reads as two blocks side by side
            // on the same row, cut together in one motion - a common real-map pattern for big
            // swings, and it makes the generated map "feel" like the size of the movement.
            if (IsWideSwing(swing.Speed, range))
            {
                var otherColumn = column == 0 ? 1 : 0;
                notes.Add(new GeneratedNote(swing.Time, columnBase + otherColumn, lineLayer, type, cutDirection));
            }
        }

        notes.Sort((a, b) => a.BeatTime.CompareTo(b.BeatTime));
        return notes;
    }

    private readonly struct HandRange
    {
        public HandRange(float minX, float maxX, float minY, float maxY, float minSpeed, float maxSpeed)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
            MinSpeed = minSpeed;
            MaxSpeed = maxSpeed;
        }

        public float MinX { get; }
        public float MaxX { get; }
        public float MinY { get; }
        public float MaxY { get; }
        public float MinSpeed { get; }
        public float MaxSpeed { get; }
    }

    /// <summary>
    /// Finds this hand's own observed range of motion across the whole recording, so grid
    /// placement adapts to how big the player's swings actually were instead of assuming they
    /// hit an absolute position in world space.
    /// </summary>
    private static HandRange ComputeRange(IReadOnlyList<SwingEvent> events, Hand hand)
    {
        var minX = float.MaxValue;
        var maxX = float.MinValue;
        var minY = float.MaxValue;
        var maxY = float.MinValue;
        var minSpeed = float.MaxValue;
        var maxSpeed = float.MinValue;
        var any = false;

        foreach (var swing in events)
        {
            if (swing.Hand != hand)
            {
                continue;
            }

            any = true;
            minX = Mathf.Min(minX, swing.Position.x);
            maxX = Mathf.Max(maxX, swing.Position.x);
            minY = Mathf.Min(minY, swing.Position.y);
            maxY = Mathf.Max(maxY, swing.Position.y);
            minSpeed = Mathf.Min(minSpeed, swing.Speed);
            maxSpeed = Mathf.Max(maxSpeed, swing.Speed);
        }

        if (!any)
        {
            return new HandRange(-0.5f, 0.5f, 0.8f, 1.6f, 0f, 0f);
        }

        // Guard against a degenerate (near-zero-width) range if the hand barely moved.
        const float minSpan = 0.05f;
        if (maxX - minX < minSpan)
        {
            maxX = minX + minSpan;
        }

        if (maxY - minY < minSpan)
        {
            maxY = minY + minSpan;
        }

        return new HandRange(minX, maxX, minY, maxY, minSpeed, maxSpeed);
    }

    /// <summary>
    /// A swing counts as "wide" if its speed is in the top 30% of this hand's own observed
    /// speed range for the whole recording - relative rather than an absolute m/s threshold,
    /// since players swing at very different intensities.
    /// </summary>
    private static bool IsWideSwing(float speed, HandRange range)
    {
        if (range.MaxSpeed <= range.MinSpeed)
        {
            return false;
        }

        const float wideSwingPercentile = 0.7f;
        return Mathf.InverseLerp(range.MinSpeed, range.MaxSpeed, speed) >= wideSwingPercentile;
    }

    private static int QuantizeColumn(Vector3 position, HandRange range)
    {
        var t = Mathf.InverseLerp(range.MinX, range.MaxX, position.x);
        return t < 0.5f ? 0 : 1;
    }

    private static int QuantizeLayer(Vector3 position, HandRange range)
    {
        var t = Mathf.InverseLerp(range.MinY, range.MaxY, position.y);
        if (t < 1f / 3f)
        {
            return 0;
        }

        return t < 2f / 3f ? 1 : 2;
    }
}

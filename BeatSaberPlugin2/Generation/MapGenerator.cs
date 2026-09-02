using System.Collections.Generic;
using System.Linq;
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

            // A swing that's unusually fast/wide for this hand reads as two blocks cut together
            // in one motion - a common real-map pattern for big swings. The pair must be laid
            // out *along* the swing's direction (e.g. stacked vertically for a Down swing), not
            // across it, since that's the only arrangement one straight swing can actually cut.
            if (IsWideSwing(swing.Speed, range))
            {
                var (columnOffset, layerOffset) = GetPairOffset(swing.Direction);
                var secondColumn = OffsetWithReflection(column, columnOffset, maxValue: 1);
                var secondLayer = OffsetWithReflection(lineLayer, layerOffset, maxValue: 2);
                notes.Add(new GeneratedNote(swing.Time, columnBase + secondColumn, secondLayer, type, cutDirection));
            }
        }

        notes.Sort((a, b) => a.BeatTime.CompareTo(b.BeatTime));
        return EnforcePlayableAlternation(notes);
    }

    /// <summary>
    /// A real saber swing that cuts a note has to swing back before it can cut another note in
    /// the *same* direction - so if two of a hand's notes land close together with the same cut
    /// direction, there isn't time for that recovery swing. Rather than leaving an unplayable
    /// pattern, close-together same-direction notes are flipped to the opposite direction, which
    /// is exactly what a natural back-and-forth swing produces. Notes that are too close to be
    /// hit at all (regardless of direction) are dropped. Simultaneous notes from the same swing
    /// (a "wide swing" pair, sharing an exact timestamp) are one physical motion and are always
    /// left alone.
    /// </summary>
    private static List<GeneratedNote> EnforcePlayableAlternation(List<GeneratedNote> notes)
    {
        const float minNoteSpacingSeconds = 0.2f;
        const float alternationWindowSeconds = 0.5f;

        var result = new List<GeneratedNote>(notes.Count);
        foreach (var type in new[] { 0, 1 })
        {
            var handGroups = GroupByTime(notes.Where(n => n.Type == type).OrderBy(n => n.BeatTime).ToList());

            float? lastTime = null;
            int? lastDirection = null;
            foreach (var group in handGroups)
            {
                var time = group[0].BeatTime;
                var direction = group[0].CutDirection;
                var gap = lastTime.HasValue ? time - lastTime.Value : float.PositiveInfinity;

                if (gap < minNoteSpacingSeconds)
                {
                    continue; // too close to the previous swing to be physically playable at all
                }

                if (gap < alternationWindowSeconds && direction == lastDirection)
                {
                    direction = OppositeCutDirection(direction);
                }

                foreach (var note in group)
                {
                    result.Add(new GeneratedNote(note.BeatTime, note.LineIndex, note.LineLayer, note.Type, direction));
                }

                lastTime = time;
                lastDirection = direction;
            }
        }

        result.Sort((a, b) => a.BeatTime.CompareTo(b.BeatTime));
        return result;
    }

    /// <summary>Groups already time-sorted notes that share the same swing (identical timestamp - a wide-swing pair).</summary>
    private static List<List<GeneratedNote>> GroupByTime(List<GeneratedNote> sortedNotes)
    {
        var groups = new List<List<GeneratedNote>>();
        foreach (var note in sortedNotes)
        {
            if (groups.Count > 0 && Mathf.Approximately(groups[^1][0].BeatTime, note.BeatTime))
            {
                groups[^1].Add(note);
            }
            else
            {
                groups.Add(new List<GeneratedNote> { note });
            }
        }

        return groups;
    }

    /// <summary>Matches Beat Saber's v2 _cutDirection values (0=Up ... 7=DownRight, 8=Any).</summary>
    private static int OppositeCutDirection(int cutDirection) => cutDirection switch
    {
        0 => 1, // Up <-> Down
        1 => 0,
        2 => 3, // Left <-> Right
        3 => 2,
        4 => 7, // UpLeft <-> DownRight
        7 => 4,
        5 => 6, // UpRight <-> DownLeft
        6 => 5,
        _ => cutDirection, // Any stays Any
    };

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

    /// <summary>
    /// Per-axis (column, layer) step to place a wide swing's second block along the swing's own
    /// direction. <see cref="SwingDirection.Any"/> has no meaningful axis, so it arbitrarily
    /// picks a horizontal pair rather than adding no second block at all.
    /// </summary>
    private static (int Column, int Layer) GetPairOffset(SwingDirection direction) => direction switch
    {
        SwingDirection.Up => (0, 1),
        SwingDirection.Down => (0, -1),
        SwingDirection.Left => (-1, 0),
        SwingDirection.Right => (1, 0),
        SwingDirection.UpLeft => (-1, 1),
        SwingDirection.UpRight => (1, 1),
        SwingDirection.DownLeft => (-1, -1),
        SwingDirection.DownRight => (1, -1),
        _ => (1, 0),
    };

    /// <summary>Moves <paramref name="value"/> by <paramref name="delta"/>, reflecting off the opposite edge instead of going out of [0, maxValue].</summary>
    private static int OffsetWithReflection(int value, int delta, int maxValue)
    {
        if (delta == 0)
        {
            return value;
        }

        var moved = value + delta;
        return moved < 0 || moved > maxValue ? value - delta : moved;
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

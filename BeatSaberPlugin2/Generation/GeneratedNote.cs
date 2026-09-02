namespace BeatSaberPlugin2.Generation;

/// <summary>A note ready to be serialized to Beat Saber's map format, decoupled from the JSON model itself.</summary>
internal readonly struct GeneratedNote
{
    public GeneratedNote(float beatTime, int lineIndex, int lineLayer, int type, int cutDirection)
    {
        BeatTime = beatTime;
        LineIndex = lineIndex;
        LineLayer = lineLayer;
        Type = type;
        CutDirection = cutDirection;
    }

    public float BeatTime { get; }

    /// <summary>Column, 0 (left) to 3 (right).</summary>
    public int LineIndex { get; }

    /// <summary>Row, 0 (bottom) to 2 (top).</summary>
    public int LineLayer { get; }

    /// <summary>0 = red (left saber), 1 = blue (right saber).</summary>
    public int Type { get; }

    /// <summary>Matches Beat Saber's v2 _cutDirection values (0=Up ... 7=DownRight, 8=Any).</summary>
    public int CutDirection { get; }
}

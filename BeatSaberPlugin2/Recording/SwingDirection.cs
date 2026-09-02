namespace BeatSaberPlugin2.Recording;

/// <summary>
/// The 8-way compass direction a swing moved in (plus <see cref="Any"/> for an ambiguous/too
/// small motion), independent of Beat Saber's own note-cut-direction enum so this module stays
/// decoupled from the map format until the generation step maps it over.
/// </summary>
internal enum SwingDirection
{
    Up,
    Down,
    Left,
    Right,
    UpLeft,
    UpRight,
    DownLeft,
    DownRight,
    Any,
}

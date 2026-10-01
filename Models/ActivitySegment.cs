namespace FocusTracker.Models;

/// <summary>
/// A continuous span of time during which one application (or the idle state)
/// held the foreground. Segments are the raw rows written to the database.
/// </summary>
public class ActivitySegment
{
    public long Id { get; set; }
    public string AppName { get; set; } = "";
    public string ProcessName { get; set; } = "";
    public string WindowTitle { get; set; } = "";
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public bool IsIdle { get; set; }

    public double DurationSeconds => Math.Max(0, (EndUtc - StartUtc).TotalSeconds);
}

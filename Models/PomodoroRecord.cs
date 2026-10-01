namespace FocusTracker.Models;

public enum PomodoroPhase
{
    Idle,
    Work,
    ShortBreak,
    LongBreak
}

/// <summary>A single completed or skipped pomodoro phase, persisted for stats.</summary>
public class PomodoroRecord
{
    public long Id { get; set; }
    public PomodoroPhase Phase { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public bool Completed { get; set; }

    public double DurationSeconds => Math.Max(0, (EndUtc - StartUtc).TotalSeconds);
}

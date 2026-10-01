namespace FocusTracker.Models;

/// <summary>User-configurable settings, persisted as JSON in %APPDATA%\FocusTracker.</summary>
public class AppSettings
{
    public int WorkMinutes { get; set; } = 25;
    public int ShortBreakMinutes { get; set; } = 5;
    public int LongBreakMinutes { get; set; } = 15;

    /// <summary>Number of work sessions before a long break.</summary>
    public int LongBreakInterval { get; set; } = 4;

    /// <summary>Seconds without keyboard/mouse input before time is counted as idle.</summary>
    public int IdleThresholdSeconds { get; set; } = 60;

    /// <summary>Automatically start the next phase when one completes.</summary>
    public bool AutoStartNextPhase { get; set; } = true;

    /// <summary>Play a chime when a phase completes.</summary>
    public bool SoundEnabled { get; set; } = true;

    /// <summary>Last position of the floating mini-timer (null = default corner).</summary>
    public double? MiniLeft { get; set; }
    public double? MiniTop { get; set; }
}

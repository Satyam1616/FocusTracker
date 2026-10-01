namespace FocusTracker.Models;

/// <summary>Aggregated time spent in one application for a given day.</summary>
public class AppUsage
{
    public string AppName { get; set; } = "";
    public double TotalSeconds { get; set; }

    /// <summary>Bar width relative to the busiest app (0..100).</summary>
    public double BarPercent { get; set; }

    /// <summary>Share of the day's tracked time (0..100).</summary>
    public double SharePercent { get; set; }

    public string TimeText => FormatDuration(TotalSeconds);
    public string ShareText => $"{SharePercent:0}%";

    public static string FormatDuration(double seconds)
    {
        var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        if (ts.TotalMinutes >= 1) return $"{ts.Minutes}m {ts.Seconds}s";
        return $"{ts.Seconds}s";
    }
}

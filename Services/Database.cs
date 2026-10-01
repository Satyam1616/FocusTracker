using System.IO;
using FocusTracker.Models;
using Microsoft.Data.Sqlite;

namespace FocusTracker.Services;

/// <summary>
/// Local SQLite store for activity segments and pomodoro records.
/// All data stays on disk in %APPDATA%\FocusTracker — nothing leaves the machine.
/// </summary>
public class Database
{
    private readonly string _connectionString;

    public Database()
    {
        var path = Path.Combine(SettingsStore.DataDirectory, "focustracker.db");
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
        Initialize();
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static string DateKey(DateTime localDate) => localDate.ToString("yyyy-MM-dd");

    private void Initialize()
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Segments(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  AppName TEXT NOT NULL, ProcessName TEXT NOT NULL, WindowTitle TEXT NOT NULL,
  StartUtc TEXT NOT NULL, EndUtc TEXT NOT NULL, DurationSeconds REAL NOT NULL,
  IsIdle INTEGER NOT NULL, LocalDate TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS idx_seg_date ON Segments(LocalDate);
CREATE TABLE IF NOT EXISTS Pomodoros(
  Id INTEGER PRIMARY KEY AUTOINCREMENT, Phase INTEGER NOT NULL,
  StartUtc TEXT NOT NULL, EndUtc TEXT NOT NULL, DurationSeconds REAL NOT NULL,
  Completed INTEGER NOT NULL, LocalDate TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS idx_pom_date ON Pomodoros(LocalDate);";
        cmd.ExecuteNonQuery();
    }
    public void InsertSegment(ActivitySegment s)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"INSERT INTO Segments
(AppName,ProcessName,WindowTitle,StartUtc,EndUtc,DurationSeconds,IsIdle,LocalDate)
VALUES($app,$proc,$title,$start,$end,$dur,$idle,$date)";
        cmd.Parameters.AddWithValue("$app", s.AppName);
        cmd.Parameters.AddWithValue("$proc", s.ProcessName);
        cmd.Parameters.AddWithValue("$title", s.WindowTitle);
        cmd.Parameters.AddWithValue("$start", s.StartUtc.ToString("o"));
        cmd.Parameters.AddWithValue("$end", s.EndUtc.ToString("o"));
        cmd.Parameters.AddWithValue("$dur", s.DurationSeconds);
        cmd.Parameters.AddWithValue("$idle", s.IsIdle ? 1 : 0);
        cmd.Parameters.AddWithValue("$date", DateKey(s.StartUtc.ToLocalTime()));
        cmd.ExecuteNonQuery();
    }

    public void InsertPomodoro(PomodoroRecord p)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"INSERT INTO Pomodoros
(Phase,StartUtc,EndUtc,DurationSeconds,Completed,LocalDate)
VALUES($phase,$start,$end,$dur,$done,$date)";
        cmd.Parameters.AddWithValue("$phase", (int)p.Phase);
        cmd.Parameters.AddWithValue("$start", p.StartUtc.ToString("o"));
        cmd.Parameters.AddWithValue("$end", p.EndUtc.ToString("o"));
        cmd.Parameters.AddWithValue("$dur", p.DurationSeconds);
        cmd.Parameters.AddWithValue("$done", p.Completed ? 1 : 0);
        cmd.Parameters.AddWithValue("$date", DateKey(p.StartUtc.ToLocalTime()));
        cmd.ExecuteNonQuery();
    }
    public List<AppUsage> GetAppUsageForDay(DateTime localDate)
    {
        var list = new List<AppUsage>();
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"SELECT AppName, SUM(DurationSeconds) FROM Segments
WHERE LocalDate=$d AND IsIdle=0 GROUP BY AppName HAVING SUM(DurationSeconds) >= 1
ORDER BY SUM(DurationSeconds) DESC";
        cmd.Parameters.AddWithValue("$d", DateKey(localDate));
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new AppUsage { AppName = r.GetString(0), TotalSeconds = r.GetDouble(1) });
        return list;
    }

    private double ScalarSeconds(string sql, DateTime localDate)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$d", DateKey(localDate));
        var result = cmd.ExecuteScalar();
        return result == null || result is DBNull ? 0 : Convert.ToDouble(result);
    }

    public double GetActiveSecondsForDay(DateTime localDate) =>
        ScalarSeconds("SELECT SUM(DurationSeconds) FROM Segments WHERE LocalDate=$d AND IsIdle=0", localDate);

    public double GetIdleSecondsForDay(DateTime localDate) =>
        ScalarSeconds("SELECT SUM(DurationSeconds) FROM Segments WHERE LocalDate=$d AND IsIdle=1", localDate);

    public double GetFocusSecondsForDay(DateTime localDate) =>
        ScalarSeconds("SELECT SUM(DurationSeconds) FROM Pomodoros WHERE LocalDate=$d AND Phase=1 AND Completed=1", localDate);

    public int GetCompletedWorkCountForDay(DateTime localDate)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Pomodoros WHERE LocalDate=$d AND Phase=1 AND Completed=1";
        cmd.Parameters.AddWithValue("$d", DateKey(localDate));
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>Active (non-idle) seconds per day for the trend chart, keyed by yyyy-MM-dd.</summary>
    public Dictionary<string, double> GetActiveSecondsRange(DateTime fromLocal, DateTime toLocal)
    {
        var map = new Dictionary<string, double>();
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"SELECT LocalDate, SUM(DurationSeconds) FROM Segments
WHERE IsIdle=0 AND LocalDate BETWEEN $from AND $to GROUP BY LocalDate";
        cmd.Parameters.AddWithValue("$from", DateKey(fromLocal));
        cmd.Parameters.AddWithValue("$to", DateKey(toLocal));
        using var r = cmd.ExecuteReader();
        while (r.Read())
            map[r.GetString(0)] = r.IsDBNull(1) ? 0 : r.GetDouble(1);
        return map;
    }
}

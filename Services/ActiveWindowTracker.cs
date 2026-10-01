using System.Diagnostics;
using System.Windows.Threading;
using FocusTracker.Models;
using FocusTracker.Native;

namespace FocusTracker.Services;

/// <summary>
/// Polls the foreground window once a second and records how long each app
/// (or the idle state) stays in front, writing completed segments to the database.
/// </summary>
public class ActiveWindowTracker
{
    private const int FlushEverySeconds = 30;

    private readonly Database _db;
    private readonly DispatcherTimer _timer;
    private readonly Dictionary<string, string> _nameCache = new();

    private ActivitySegment? _current;
    private int _idleThresholdSeconds;
    private DateTime _lastFlushUtc;

    /// <summary>Raised when the foreground app or idle state changes: (label, isIdle).</summary>
    public event Action<string, bool>? CurrentChanged;

    public string CurrentLabel { get; private set; } = "—";
    public bool IsIdle { get; private set; }

    public ActiveWindowTracker(Database db, int idleThresholdSeconds)
    {
        _db = db;
        _idleThresholdSeconds = Math.Max(5, idleThresholdSeconds);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Tick();
    }

    public void UpdateIdleThreshold(int seconds) => _idleThresholdSeconds = Math.Max(5, seconds);

    public void Start()
    {
        _lastFlushUtc = DateTime.UtcNow;
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        CloseCurrent(DateTime.UtcNow);
    }
    private void Tick()
    {
        var now = DateTime.UtcNow;
        bool idle = NativeMethods.IdleSeconds() >= _idleThresholdSeconds;

        string appName, processName, title;
        if (idle)
        {
            appName = "(idle)";
            processName = "idle";
            title = "";
        }
        else
        {
            ResolveForeground(out appName, out processName, out title);
        }

        string key = idle ? "idle" : processName + "|" + appName;
        string currentKey = _current == null
            ? ""
            : (_current.IsIdle ? "idle" : _current.ProcessName + "|" + _current.AppName);

        if (_current == null || key != currentKey)
        {
            CloseCurrent(now);
            _current = new ActivitySegment
            {
                AppName = appName,
                ProcessName = processName,
                WindowTitle = title,
                StartUtc = now,
                EndUtc = now,
                IsIdle = idle
            };
            CurrentLabel = idle ? "Idle" : appName;
            IsIdle = idle;
            CurrentChanged?.Invoke(CurrentLabel, idle);
        }
        else
        {
            _current.EndUtc = now;
            _current.WindowTitle = title;
        }

        if ((now - _lastFlushUtc).TotalSeconds >= FlushEverySeconds)
        {
            FlushOpen(now);
            _lastFlushUtc = now;
        }
    }
    private void FlushOpen(DateTime now)
    {
        if (_current == null) return;
        _current.EndUtc = now;
        if (_current.DurationSeconds < 1) return;

        _db.InsertSegment(_current);
        // Continue the same app in a fresh segment so totals keep accumulating.
        _current = new ActivitySegment
        {
            AppName = _current.AppName,
            ProcessName = _current.ProcessName,
            WindowTitle = _current.WindowTitle,
            StartUtc = now,
            EndUtc = now,
            IsIdle = _current.IsIdle
        };
    }

    private void CloseCurrent(DateTime now)
    {
        if (_current == null) return;
        _current.EndUtc = now;
        if (_current.DurationSeconds >= 1)
            _db.InsertSegment(_current);
        _current = null;
    }

    private void ResolveForeground(out string appName, out string processName, out string title)
    {
        appName = "Unknown";
        processName = "unknown";
        title = "";
        try
        {
            var hwnd = NativeMethods.ForegroundWindow();
            if (hwnd == IntPtr.Zero) return;
            title = NativeMethods.WindowTitle(hwnd);
            uint pid = NativeMethods.ProcessIdForWindow(hwnd);
            if (pid == 0) return;
            using var proc = Process.GetProcessById((int)pid);
            processName = proc.ProcessName;
            appName = FriendlyName(proc);
        }
        catch
        {
            // Access denied or the process exited mid-read; keep best-effort values.
        }
    }

    private string FriendlyName(Process proc)
    {
        if (_nameCache.TryGetValue(proc.ProcessName, out var cached))
            return cached;

        string name = proc.ProcessName;
        try
        {
            var desc = proc.MainModule?.FileVersionInfo.FileDescription;
            if (!string.IsNullOrWhiteSpace(desc)) name = desc!;
        }
        catch
        {
            // Bitness or permission mismatch reading another process module.
        }
        _nameCache[proc.ProcessName] = name;
        return name;
    }
}

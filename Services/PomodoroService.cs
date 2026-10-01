using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using FocusTracker.Models;

namespace FocusTracker.Services;

/// <summary>
/// Drives the pomodoro timer: countdown, phase transitions (work → short/long break),
/// completion sounds, and persistence of completed work sessions.
/// Implements <see cref="INotifyPropertyChanged"/> so the UI can bind directly.
/// </summary>
public class PomodoroService : INotifyPropertyChanged
{
    private readonly Database _db;
    private readonly DispatcherTimer _timer;
    private readonly ChimePlayer _chimes = new();
    private AppSettings _settings;

    private PomodoroPhase _phase = PomodoroPhase.Work;
    private int _remainingSeconds;
    private int _phaseLengthSeconds;
    private bool _isRunning;
    private int _workSessionsSinceLongBreak;
    private int _completedToday;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raised when a phase finishes on its own: (completedPhase, nextPhase).</summary>
    public event Action<PomodoroPhase, PomodoroPhase>? PhaseCompleted;

    public PomodoroService(Database db, AppSettings settings)
    {
        _db = db;
        _settings = settings;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Tick();
        _completedToday = _db.GetCompletedWorkCountForDay(DateTime.Now);
        SetPhase(PomodoroPhase.Work);
    }

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        if (!_isRunning) SetPhase(_phase); // refresh the current phase length live
    }
    public PomodoroPhase Phase => _phase;
    public bool IsRunning => _isRunning;
    public int CompletedToday => _completedToday;

    public string TimeText => TimeSpan.FromSeconds(_remainingSeconds).ToString(@"mm\:ss");

    public string PhaseText => _phase switch
    {
        PomodoroPhase.Work => "Focus",
        PomodoroPhase.ShortBreak => "Short Break",
        PomodoroPhase.LongBreak => "Long Break",
        _ => "Idle"
    };

    public double ProgressFraction =>
        _phaseLengthSeconds <= 0 ? 0 : 1.0 - (double)_remainingSeconds / _phaseLengthSeconds;

    public void Start()
    {
        if (_isRunning) return;
        if (_remainingSeconds <= 0) SetPhase(_phase);
        _isRunning = true;
        _timer.Start();
        OnChanged(nameof(IsRunning));
    }

    public void Pause()
    {
        if (!_isRunning) return;
        _isRunning = false;
        _timer.Stop();
        OnChanged(nameof(IsRunning));
    }

    /// <summary>Reset the current phase back to its full length.</summary>
    public void Reset()
    {
        _isRunning = false;
        _timer.Stop();
        SetPhase(_phase);
        OnChanged(nameof(IsRunning));
    }

    /// <summary>Skip to the next phase without recording a completion.</summary>
    public void Skip()
    {
        _isRunning = false;
        _timer.Stop();
        var next = _phase == PomodoroPhase.Work ? PomodoroPhase.ShortBreak : PomodoroPhase.Work;
        SetPhase(next);
        OnChanged(nameof(IsRunning));
    }
    private void Tick()
    {
        _remainingSeconds--;
        if (_remainingSeconds <= 0)
        {
            _remainingSeconds = 0;
            RaiseTimeProps();
            CompleteCurrentPhase();
            return;
        }
        RaiseTimeProps();
    }

    private void CompleteCurrentPhase()
    {
        _timer.Stop();
        _isRunning = false;

        var completed = _phase;
        RecordCompletion(completed);

        if (_settings.SoundEnabled)
        {
            if (completed == PomodoroPhase.Work) _chimes.PlayFocusFinished();
            else _chimes.PlayBreakFinished();
        }

        var next = DetermineNextPhase(completed);
        PhaseCompleted?.Invoke(completed, next);

        SetPhase(next);
        OnChanged(nameof(IsRunning));

        if (_settings.AutoStartNextPhase) Start();
    }

    private void RecordCompletion(PomodoroPhase completed)
    {
        var now = DateTime.UtcNow;
        var length = PhaseLengthSeconds(completed);
        _db.InsertPomodoro(new PomodoroRecord
        {
            Phase = completed,
            StartUtc = now.AddSeconds(-length),
            EndUtc = now,
            Completed = true
        });

        if (completed == PomodoroPhase.Work)
        {
            _workSessionsSinceLongBreak++;
            _completedToday++;
            OnChanged(nameof(CompletedToday));
        }
    }

    private PomodoroPhase DetermineNextPhase(PomodoroPhase completed)
    {
        if (completed == PomodoroPhase.Work)
        {
            bool longDue = _workSessionsSinceLongBreak % Math.Max(1, _settings.LongBreakInterval) == 0;
            return longDue ? PomodoroPhase.LongBreak : PomodoroPhase.ShortBreak;
        }
        if (completed == PomodoroPhase.LongBreak) _workSessionsSinceLongBreak = 0;
        return PomodoroPhase.Work;
    }
    private int PhaseLengthSeconds(PomodoroPhase phase) => phase switch
    {
        PomodoroPhase.Work => Math.Max(1, _settings.WorkMinutes) * 60,
        PomodoroPhase.ShortBreak => Math.Max(1, _settings.ShortBreakMinutes) * 60,
        PomodoroPhase.LongBreak => Math.Max(1, _settings.LongBreakMinutes) * 60,
        _ => 0
    };

    private void SetPhase(PomodoroPhase phase)
    {
        _phase = phase;
        _phaseLengthSeconds = PhaseLengthSeconds(phase);
        _remainingSeconds = _phaseLengthSeconds;
        OnChanged(nameof(Phase));
        OnChanged(nameof(PhaseText));
        RaiseTimeProps();
    }

    private void RaiseTimeProps()
    {
        OnChanged(nameof(TimeText));
        OnChanged(nameof(ProgressFraction));
    }

    private void OnChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

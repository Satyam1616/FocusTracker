using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FocusTracker.Models;
using FocusTracker.Services;

namespace FocusTracker.Views;

public partial class TimerView : UserControl
{
    private readonly PomodoroService _pomodoro;
    private readonly ActiveWindowTracker _tracker;
    private readonly Action _onMiniMode;

    public TimerView(PomodoroService pomodoro, ActiveWindowTracker tracker, Action onMiniMode)
    {
        InitializeComponent();
        _pomodoro = pomodoro;
        _tracker = tracker;
        _onMiniMode = onMiniMode;
        DataContext = pomodoro;

        _pomodoro.PropertyChanged += OnPomodoroChanged;
        _tracker.CurrentChanged += OnTrackerChanged;

        UpdateButton();
        UpdateCount();
        UpdatePhaseColor();
        CurrentRun.Text = _tracker.CurrentLabel;
    }

    private void OnPomodoroChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PomodoroService.IsRunning)) UpdateButton();
        else if (e.PropertyName == nameof(PomodoroService.CompletedToday)) UpdateCount();
        else if (e.PropertyName == nameof(PomodoroService.Phase)) { UpdatePhaseColor(); UpdateButton(); }
    }

    private void OnTrackerChanged(string label, bool isIdle) => CurrentRun.Text = label;

    private void UpdateButton() => StartBtn.Content = _pomodoro.IsRunning ? "Pause" : "Start";

    private void UpdateCount() => CountRun.Text = " " + _pomodoro.CompletedToday;

    private void UpdatePhaseColor()
    {
        var key = _pomodoro.Phase == PomodoroPhase.Work ? "AccentBrush" : "BreakBrush";
        Bar.Foreground = (Brush)Application.Current.Resources[key];
    }

    private void StartPause_Click(object sender, RoutedEventArgs e)
    {
        if (_pomodoro.IsRunning) _pomodoro.Pause();
        else _pomodoro.Start();
    }

    private void Reset_Click(object sender, RoutedEventArgs e) => _pomodoro.Reset();

    private void Skip_Click(object sender, RoutedEventArgs e) => _pomodoro.Skip();

    private void Mini_Click(object sender, RoutedEventArgs e) => _onMiniMode();
}

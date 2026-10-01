using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FocusTracker.Models;
using FocusTracker.Services;

namespace FocusTracker;

/// <summary>Always-on-top, draggable floating timer (picture-in-picture mode).</summary>
public partial class MiniTimerWindow : Window
{
    private readonly PomodoroService _pomodoro;
    private readonly Action _onRestore;

    public MiniTimerWindow(PomodoroService pomodoro, Action onRestore)
    {
        InitializeComponent();
        _pomodoro = pomodoro;
        _onRestore = onRestore;
        DataContext = pomodoro;

        _pomodoro.PropertyChanged += OnPomodoroChanged;
        UpdateButton();
        UpdatePhaseColor();
    }

    private void OnPomodoroChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PomodoroService.IsRunning)) UpdateButton();
        else if (e.PropertyName == nameof(PomodoroService.Phase)) UpdatePhaseColor();
    }

    private void UpdateButton() => StartBtn.Content = _pomodoro.IsRunning ? "Pause" : "Start";

    private void UpdatePhaseColor()
    {
        var brush = (Brush)Application.Current.Resources[
            _pomodoro.Phase == PomodoroPhase.Work ? "AccentBrush" : "BreakBrush"];
        Bar.Foreground = brush;
        PhaseLabel.Foreground = brush;
    }

    private void Root_Drag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void StartPause_Click(object sender, RoutedEventArgs e)
    {
        if (_pomodoro.IsRunning) _pomodoro.Pause();
        else _pomodoro.Start();
    }

    private void Skip_Click(object sender, RoutedEventArgs e) => _pomodoro.Skip();

    private void Restore_Click(object sender, RoutedEventArgs e) => _onRestore();

    protected override void OnClosed(EventArgs e)
    {
        _pomodoro.PropertyChanged -= OnPomodoroChanged;
        base.OnClosed(e);
    }
}

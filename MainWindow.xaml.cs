using System.ComponentModel;
using System.Windows;
using FocusTracker.Models;
using FocusTracker.Services;
using FocusTracker.Views;

namespace FocusTracker;

public partial class MainWindow : Window
{
    private readonly Database _db;
    private readonly ActiveWindowTracker _tracker;
    private readonly PomodoroService _pomodoro;
    private AppSettings _settings;

    private readonly DashboardView _dashboard;
    private MiniTimerWindow? _mini;

    public MainWindow()
    {
        InitializeComponent();

        _settings = SettingsStore.Load();
        _db = new Database();
        _tracker = new ActiveWindowTracker(_db, _settings.IdleThresholdSeconds);
        _pomodoro = new PomodoroService(_db, _settings);

        TimerHost.Content = new TimerView(_pomodoro, _tracker, EnterMiniMode);
        _dashboard = new DashboardView(_db);
        DashHost.Content = _dashboard;
        SettingsHost.Content = new SettingsView(_settings, OnSettingsSaved);

        _tracker.Start();
        Closing += OnClosing;
    }

    private void OnSettingsSaved(AppSettings updated)
    {
        // Preserve state the settings form doesn't edit.
        updated.MiniLeft = _settings.MiniLeft;
        updated.MiniTop = _settings.MiniTop;

        _settings = updated;
        SettingsStore.Save(updated);
        _tracker.UpdateIdleThreshold(updated.IdleThresholdSeconds);
        _pomodoro.ApplySettings(updated);
    }

    private void EnterMiniMode()
    {
        if (_mini == null)
        {
            _mini = new MiniTimerWindow(_pomodoro, RestoreFromMini);
            _mini.Closed += (_, _) =>
            {
                _mini = null;
                if (!IsVisible) RestoreMainWindow(); // avoid leaving no window visible
            };
        }
        _mini.Show();
        PositionMini();
        _mini.Activate();
        Hide();
    }

    private void RestoreFromMini()
    {
        SaveMiniPosition();
        _mini?.Hide();
        RestoreMainWindow();
    }

    private void RestoreMainWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void PositionMini()
    {
        if (_mini == null) return;
        var wa = SystemParameters.WorkArea;

        if (_settings.MiniLeft is double l && _settings.MiniTop is double t &&
            l >= wa.Left && t >= wa.Top && l < wa.Right - 20 && t < wa.Bottom - 20)
        {
            _mini.Left = l;
            _mini.Top = t;
        }
        else
        {
            _mini.Left = wa.Right - _mini.ActualWidth - 24;
            _mini.Top = wa.Bottom - _mini.ActualHeight - 24;
        }
    }

    private void SaveMiniPosition()
    {
        if (_mini == null) return;
        _settings.MiniLeft = _mini.Left;
        _settings.MiniTop = _mini.Top;
        SettingsStore.Save(_settings);
    }

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (TimerHost == null) return; // fires once during InitializeComponent before fields set

        TimerHost.Visibility = NavTimer.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        DashHost.Visibility = NavDash.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        SettingsHost.Visibility = NavSettings.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

        if (NavDash.IsChecked == true)
            _dashboard.Refresh();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        SaveMiniPosition();
        _mini?.Close();
        _tracker.Stop(); // flush the open activity segment to disk
    }
}

using System.Windows;
using System.Windows.Controls;
using FocusTracker.Models;

namespace FocusTracker.Views;

public partial class SettingsView : UserControl
{
    private readonly Action<AppSettings> _onSaved;

    public SettingsView(AppSettings settings, Action<AppSettings> onSaved)
    {
        InitializeComponent();
        _onSaved = onSaved;

        WorkBox.Text = settings.WorkMinutes.ToString();
        ShortBox.Text = settings.ShortBreakMinutes.ToString();
        LongBox.Text = settings.LongBreakMinutes.ToString();
        IntervalBox.Text = settings.LongBreakInterval.ToString();
        IdleBox.Text = settings.IdleThresholdSeconds.ToString();
        AutoChk.IsChecked = settings.AutoStartNextPhase;
        SoundChk.IsChecked = settings.SoundEnabled;
    }

    private static int ParseClamp(TextBox box, int min, int max, int fallback)
    {
        if (!int.TryParse(box.Text, out int value)) value = fallback;
        value = Math.Clamp(value, min, max);
        box.Text = value.ToString();
        return value;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var settings = new AppSettings
        {
            WorkMinutes = ParseClamp(WorkBox, 1, 180, 25),
            ShortBreakMinutes = ParseClamp(ShortBox, 1, 60, 5),
            LongBreakMinutes = ParseClamp(LongBox, 1, 120, 15),
            LongBreakInterval = ParseClamp(IntervalBox, 1, 12, 4),
            IdleThresholdSeconds = ParseClamp(IdleBox, 10, 3600, 60),
            AutoStartNextPhase = AutoChk.IsChecked == true,
            SoundEnabled = SoundChk.IsChecked == true
        };

        _onSaved(settings);
        SavedMsg.Text = "Saved ✓";
    }
}

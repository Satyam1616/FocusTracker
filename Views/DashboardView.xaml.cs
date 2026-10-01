using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FocusTracker.Models;
using FocusTracker.Services;

namespace FocusTracker.Views;

public partial class DashboardView : UserControl
{
    private readonly Database _db;
    private DateTime _date = DateTime.Today;

    public DashboardView(Database db)
    {
        InitializeComponent();
        _db = db;
        Refresh();
    }

    private void Prev_Click(object sender, RoutedEventArgs e)
    {
        _date = _date.AddDays(-1);
        Refresh();
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        if (_date.Date >= DateTime.Today) return; // don't navigate into the future
        _date = _date.AddDays(1);
        Refresh();
    }

    private void Today_Click(object sender, RoutedEventArgs e)
    {
        _date = DateTime.Today;
        Refresh();
    }
    /// <summary>Reload all widgets for the currently selected day.</summary>
    public void Refresh()
    {
        DateLabel.Text = _date.Date == DateTime.Today ? "Today" : _date.ToString("ddd, dd MMM");

        double active = _db.GetActiveSecondsForDay(_date);
        ActiveText.Text = AppUsage.FormatDuration(active);
        FocusText.Text = AppUsage.FormatDuration(_db.GetFocusSecondsForDay(_date));
        SessionsText.Text = _db.GetCompletedWorkCountForDay(_date).ToString();
        IdleText.Text = AppUsage.FormatDuration(_db.GetIdleSecondsForDay(_date));

        var apps = _db.GetAppUsageForDay(_date);
        double max = apps.Count > 0 ? apps[0].TotalSeconds : 0;
        foreach (var a in apps)
        {
            a.BarPercent = max > 0 ? a.TotalSeconds / max * 100 : 0;
            a.SharePercent = active > 0 ? a.TotalSeconds / active * 100 : 0;
        }

        var top = apps.Take(8).ToList();
        AppsList.ItemsSource = top;
        EmptyApps.Visibility = top.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        BuildTrend();
    }
    private void BuildTrend()
    {
        TrendPanel.Children.Clear();
        var from = DateTime.Today.AddDays(-6);
        var data = _db.GetActiveSecondsRange(from, DateTime.Today);
        double max = data.Count > 0 ? data.Values.Max() : 0;

        var accent = (Brush)Application.Current.Resources["AccentBrush"];
        var track = (Brush)Application.Current.Resources["TrackBrush"];
        var muted = (Brush)Application.Current.Resources["MutedBrush"];

        for (int i = 0; i < 7; i++)
        {
            var day = from.AddDays(i);
            data.TryGetValue(day.ToString("yyyy-MM-dd"), out double secs);
            double height = max > 0 ? Math.Max(4, secs / max * 96) : 4;

            var bars = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom };
            bars.Children.Add(new TextBlock
            {
                Text = secs >= 60 ? $"{(int)(secs / 60)}m" : "",
                Foreground = muted, FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 5)
            });
            bars.Children.Add(new Border
            {
                Height = height, Width = 26, CornerRadius = new CornerRadius(6),
                Background = day.Date == _date.Date ? accent : track
            });

            var col = new Grid { Width = 56 };
            col.RowDefinitions.Add(new RowDefinition());
            col.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(bars, 0);
            col.Children.Add(bars);

            var label = new TextBlock
            {
                Text = day.ToString("ddd"), Foreground = muted, FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 0)
            };
            Grid.SetRow(label, 1);
            col.Children.Add(label);

            TrendPanel.Children.Add(col);
        }
    }
}

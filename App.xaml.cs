using System.Windows;
using Velopack;

namespace FocusTracker;

/// <summary>Interaction logic for App.xaml</summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Velopack handles install/update hooks; must run before any UI is shown.
        VelopackApp.Build().Run();
        base.OnStartup(e);
    }
}

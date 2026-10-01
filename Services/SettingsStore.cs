using System.IO;
using System.Text.Json;
using FocusTracker.Models;

namespace FocusTracker.Services;

/// <summary>Loads and saves <see cref="AppSettings"/> as JSON, and owns the app data folder.</summary>
public static class SettingsStore
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FocusTracker");

    private static readonly string FilePath = Path.Combine(Dir, "settings.json");

    public static string DataDirectory
    {
        get
        {
            Directory.CreateDirectory(Dir);
            return Dir;
        }
    }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null) return settings;
            }
        }
        catch
        {
            // Corrupt or unreadable settings fall back to sane defaults.
        }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Dir);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}

using System.Text.Json;

namespace SeeGreen;

public class Preferences
{
    public int PanelLeft { get; set; } = 100;
    public int PanelTop { get; set; } = 100;
    public int ZoomFactor { get; set; } = 2;
    public bool MagnifierActive { get; set; } = false;
    public bool FollowCursor { get; set; } = true;
    public bool Smoothing { get; set; } = true;
    public bool Crosshair { get; set; } = true;
    public bool CaptureCrosshair { get; set; } = false;
    public int MagnifierWidth { get; set; } = 600;
    public int MagnifierHeight { get; set; } = 400;

    private static string SettingsPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SeeGreen", "settings.json");

    public static Preferences Load()
    {
        try
        {
            var path = SettingsPath;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var prefs = JsonSerializer.Deserialize<Preferences>(json);
                if (prefs != null) return prefs;
            }
        }
        catch { }
        return new Preferences();
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }

    public void ResetToDefaults()
    {
        PanelLeft = 100;
        PanelTop = 100;
        ZoomFactor = 2;
        MagnifierActive = false;
        FollowCursor = true;
        Smoothing = true;
        Crosshair = true;
        CaptureCrosshair = false;
        MagnifierWidth = 600;
        MagnifierHeight = 400;
    }
}
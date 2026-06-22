using System.Text.Json;

namespace HashCode;

public sealed class AppSettings
{
    public string GoldenDirectory { get; set; } = @"C:\HashCode\Golden";
    public string GoldenFileName { get; set; } = string.Empty;
    public string TargetDirectory { get; set; } = @"C:\HashCode\UnCheck";
    public string CustomTargetDirectory { get; set; } = string.Empty;
    public string TargetFileName { get; set; } = string.Empty;
    public string TargetPathPreset { get; set; } = "自訂";
    public bool AllowDifferentFileNames { get; set; }
    public string LogDirectory { get; set; } = @"C:\HashCode\logs";
    public string LogNamePrefix { get; set; } = "log";
    public List<string> IgnoredEntries { get; set; } = [];
    public List<string> LastGoldenEntries { get; set; } = [];

    public static string SettingsPath =>
        Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(AppContext.BaseDirectory);

        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(SettingsPath, json);
    }
}

using System.IO;
using System.Text.Json;
using Grimoire.Core.Interfaces;
using Grimoire.Core.Models;

namespace Grimoire.App.Services;

/// <summary>
/// Persists game settings to a JSON file in %LocalAppData%\GrimoireAstralArchitect.
/// Survives app restarts without touching the SQLite database.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private const string FileName = "settings.json";
    private readonly string _path;

    public GameSettings Settings { get; private set; } = new();

    public SettingsService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GrimoireAstralArchitect");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, FileName);
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(_path)) return;

            var json = File.ReadAllText(_path);
            var loaded = JsonSerializer.Deserialize<GameSettings>(json);
            if (loaded is not null)
                Settings = loaded;
        }
        catch
        {
            // Fall back to defaults on corrupt settings file
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_path, json);
        }
        catch
        {
            // Swallow settings save failures
        }
    }
}

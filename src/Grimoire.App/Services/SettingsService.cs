using Grimoire.Core.Interfaces;
using Grimoire.Core.Models;
using Windows.Storage;

namespace Grimoire.App.Services;

/// <summary>
/// Persists game settings via Windows ApplicationData local settings.
/// Survives app restarts without touching the SQLite database.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private const string SettingsContainerName = "GrimoireSettings";
    private readonly ApplicationDataContainer _container;

    public GameSettings Settings { get; private set; } = new();

    public SettingsService()
    {
        _container = ApplicationData.Current.LocalSettings.CreateContainer(
            SettingsContainerName, ApplicationDataCreateDisposition.Always);
    }

    public void Load()
    {
        Settings = new GameSettings
        {
            TargetFps = ReadInt("TargetFps", 60),
            ParticleDensity = ReadInt("ParticleDensity", 100),
            MusicVolume = ReadFloat("MusicVolume", 0.7f),
            SfxVolume = ReadFloat("SfxVolume", 0.8f),
            ShowTutorialHints = ReadBool("ShowTutorialHints", true),
            ShowNotifications = ReadBool("ShowNotifications", true),
            EnableGestureTrail = ReadBool("EnableGestureTrail", true),
            WindowX = ReadInt("WindowX", -1),
            WindowY = ReadInt("WindowY", -1),
            WindowWidth = ReadInt("WindowWidth", 1400),
            WindowHeight = ReadInt("WindowHeight", 800),
            IsMaximized = ReadBool("IsMaximized", false)
        };
    }

    public void Save()
    {
        WriteInt("TargetFps", Settings.TargetFps);
        WriteInt("ParticleDensity", Settings.ParticleDensity);
        WriteFloat("MusicVolume", Settings.MusicVolume);
        WriteFloat("SfxVolume", Settings.SfxVolume);
        WriteBool("ShowTutorialHints", Settings.ShowTutorialHints);
        WriteBool("ShowNotifications", Settings.ShowNotifications);
        WriteBool("EnableGestureTrail", Settings.EnableGestureTrail);
        WriteInt("WindowX", Settings.WindowX);
        WriteInt("WindowY", Settings.WindowY);
        WriteInt("WindowWidth", Settings.WindowWidth);
        WriteInt("WindowHeight", Settings.WindowHeight);
        WriteBool("IsMaximized", Settings.IsMaximized);
    }

    private int ReadInt(string key, int fallback) =>
        _container.Values[key] is int v ? v : fallback;

    private float ReadFloat(string key, float fallback) =>
        _container.Values[key] is double d ? (float)d : fallback;

    private bool ReadBool(string key, bool fallback) =>
        _container.Values[key] is bool b ? b : fallback;

    private void WriteInt(string key, int value) => _container.Values[key] = value;
    private void WriteFloat(string key, float value) => _container.Values[key] = (double)value;
    private void WriteBool(string key, bool value) => _container.Values[key] = value;
}

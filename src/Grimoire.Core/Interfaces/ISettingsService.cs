using Grimoire.Core.Models;

namespace Grimoire.Core.Interfaces;

/// <summary>
/// Manages persistent game settings via Windows ApplicationData local settings.
/// </summary>
public interface ISettingsService
{
    GameSettings Settings { get; }
    void Load();
    void Save();
}

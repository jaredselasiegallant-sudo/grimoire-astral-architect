using Grimoire.Core.Interfaces;

namespace Grimoire.App.Services;

/// <summary>
/// Handles application lifecycle save/load triggers.
/// Called by App.xaml.cs on launch and exit.
/// </summary>
public static class SaveLoadService
{
    private static IGameStateService? _stateService;

    /// <summary>Initialise with the active game state service.</summary>
    public static void Initialise(IGameStateService stateService)
    {
        _stateService = stateService;
    }

    /// <summary>Save the current game state to SQLite.</summary>
    public static async Task SaveOnExitAsync()
    {
        if (_stateService is null) return;
        try
        {
            await _stateService.SaveAsync();
        }
        catch
        {
            // Silently handle save failures on exit.
        }
    }

    /// <summary>
    /// Start a periodic auto-save timer.
    /// Returns the timer so the caller can dispose it on shutdown.
    /// </summary>
    public static Timer StartAutoSaveTimer(TimeSpan interval)
    {
        return new Timer(async _ =>
        {
            if (_stateService is not null)
                await _stateService.SaveAsync();
        }, null, interval, interval);
    }
}

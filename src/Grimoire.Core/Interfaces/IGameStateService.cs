using Grimoire.Core.Enums;
using Grimoire.Core.Models;

namespace Grimoire.Core.Interfaces;

/// <summary>
/// Orchestrates saving/loading the full game state and computing
/// offline idle rewards on application launch.
/// </summary>
public interface IGameStateService
{
    GameState CurrentState { get; }

    /// <summary>True once <see cref="InitialiseAsync"/> has completed.</summary>
    bool IsInitialised { get; }

    Task InitialiseAsync();
    Task SaveAsync();

    /// <summary>
    /// Calculates and applies all idle expedition rewards that accrued
    /// while the game was closed, based on system clock delta.
    /// </summary>
    List<ExpeditionResult> CalculateOfflineRewards();

    bool IsSpellReady(SpellGesture gesture);
    void PutSpellOnCooldown(SpellGesture gesture, TimeSpan cooldown);
    TimeSpan GetSpellCooldownRemaining(SpellGesture gesture);
    void TickManaRegen(float deltaTimeSeconds);
    void TickCorruption(float deltaTimeSeconds);
    List<FamiliarEgg> CheckAndHatchEggs();
    ComboTracker GetComboTracker();
}

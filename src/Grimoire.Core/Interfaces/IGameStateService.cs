using Grimoire.Core.Models;

namespace Grimoire.Core.Interfaces;

/// <summary>
/// Orchestrates saving/loading the full game state and computing
/// offline idle rewards on application launch.
/// </summary>
public interface IGameStateService
{
    GameState CurrentState { get; }

    Task InitialiseAsync();
    Task SaveAsync();

    /// <summary>
    /// Calculates and applies all idle expedition rewards that accrued
    /// while the game was closed, based on system clock delta.
    /// </summary>
    List<ExpeditionResult> CalculateOfflineRewards();
}

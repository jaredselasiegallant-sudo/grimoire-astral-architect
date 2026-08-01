using Grimoire.Core.Models;
using Grimoire.Core.Bonding;
using Grimoire.Core.Grimoire;
using Grimoire.Core.Decay;
using Grimoire.Core.Cosmetics;
using Grimoire.Core.Accessibility;
using Grimoire.Core.Events;

namespace Grimoire.Core.Interfaces;

/// <summary>
/// Abstraction over the SQLite persistence layer.
/// </summary>
public interface IGameRepository
{
    Task<GameState?> LoadGameStateAsync();
    Task SaveGameStateAsync(GameState state);

    Task<List<Familiar>> GetFamiliarsAsync();
    Task UpsertFamiliarAsync(Familiar familiar);
    Task DeleteFamiliarAsync(Guid id);

    Task<List<InventoryItem>> GetInventoryAsync();
    Task UpsertInventoryItemAsync(InventoryItem item);
    Task DeleteInventoryItemAsync(Guid id);

    Task<List<SanctuaryBuilding>> GetBuildingsAsync();
    Task UpsertBuildingAsync(SanctuaryBuilding building);
    Task DeleteBuildingAsync(Guid id);

    Task<List<FamiliarEgg>> GetEggsAsync();
    Task UpsertEggAsync(FamiliarEgg egg);

    Task<List<CraftingRecipe>> GetRecipesAsync();
    Task UpsertRecipeAsync(CraftingRecipe recipe);

    Task<List<ExpeditionLogEntry>> GetExpeditionLogAsync();
    Task InsertExpeditionLogAsync(ExpeditionLogEntry entry);

    Task<List<string>> GetShownNarrativeLinesAsync();
    Task InsertNarrativeLineAsync(string lineId);

    Task<List<string>> GetCompletedTutorialStepsAsync();
    Task InsertTutorialStepAsync(string stepId);

    // v3 methods
    Task<Dictionary<Guid, FamiliarBond>> GetBondsAsync();
    Task UpsertBondAsync(FamiliarBond bond);

    Task<GrimoireJournal> LoadGrimoireJournalAsync();
    Task<List<GrimoireEntry>> GetGrimoireEntriesAsync();
    Task UpsertGrimoireEntryAsync(GrimoireEntry entry);

    Task<CorruptionState> GetCorruptionAsync();
    Task UpsertCorruptionAsync(CorruptionState corruption);

    Task<List<CosmeticItem>> GetCosmeticsAsync();
    Task UpsertCosmeticAsync(CosmeticItem item);
    Task<CosmeticLoadout> GetLoadoutAsync();
    Task UpsertLoadoutAsync(CosmeticLoadout loadout);

    Task<List<PlayerSpell>> GetPlayerSpellsAsync();
    Task UpsertPlayerSpellAsync(PlayerSpell spell);

    Task<AccessibilitySettings> GetAccessibilityAsync();
    Task UpsertAccessibilityAsync(AccessibilitySettings settings);

    Task<List<AstralEvent>> GetAstralEventsAsync();
    Task UpsertAstralEventAsync(AstralEvent evt);
    Task DeleteExpiredEventsAsync();
}

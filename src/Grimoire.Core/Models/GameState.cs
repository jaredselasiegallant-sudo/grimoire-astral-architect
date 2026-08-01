using Grimoire.Core.Enums;
using Grimoire.Core.Models;
using Grimoire.Core.Bonding;
using Grimoire.Core.Events;
using Grimoire.Core.Grimoire;
using Grimoire.Core.Decay;
using Grimoire.Core.Cosmetics;
using Grimoire.Core.Accessibility;
using Grimoire.Core.Weather;
using Grimoire.Core.Memories;
using Grimoire.Core.Signature;
using Grimoire.Core.Chronicle;

namespace Grimoire.Core.Models;

/// <summary>
/// Serialisable snapshot of the entire game state.
/// Persisted to SQLite on exit and restored on launch.
/// </summary>
public sealed class GameState
{
    public int SchemaVersion { get; set; } = 4;
    public Guid PlayerId { get; init; } = Guid.NewGuid();
    public string PlayerName { get; set; } = "Architect";
    public int ManaCrystals { get; set; }
    public int ManaRegenRate { get; set; }
    public int TotalPlayTimeSeconds { get; set; }
    public DateTimeOffset LastSaveUTC { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset FirstLaunchUTC { get; set; } = DateTimeOffset.UtcNow;

    // Core collections
    public List<Familiar> Familiars { get; set; } = [];
    public List<InventoryItem> Inventory { get; set; } = [];
    public List<SanctuaryBuilding> Buildings { get; set; } = [];
    public List<FamiliarEgg> Eggs { get; set; } = [];
    public List<CraftingRecipe> Recipes { get; set; } = [];
    public List<ExpeditionLogEntry> ExpeditionLog { get; set; } = [];
    public List<string> ShownNarrativeLines { get; set; } = [];
    public List<string> CompletedTutorialSteps { get; set; } = [];
    public bool TutorialCompleted { get; set; }

    // v2 systems
    public Dictionary<Guid, FamiliarBond> FamiliarBonds { get; set; } = [];
    public GrimoireJournal Grimoire { get; set; } = new();
    public CorruptionState Corruption { get; set; } = new();
    public CosmeticLoadout Cosmetics { get; set; } = new();
    public List<CosmeticItem> OwnedCosmetics { get; set; } = [];
    public List<PlayerSpell> PlayerSpells { get; set; } = [];
    public AccessibilitySettings Accessibility { get; set; } = new();
    public List<AstralEvent> ActiveEvents { get; set; } = [];
    public int SanctuaryLevel { get; set; } = 1;
    public int TotalExpeditionsCompleted { get; set; }
    public int TotalSpellsCast { get; set; }
    public int TotalRecipesDiscovered { get; set; }

    // v3 systems
    public WeatherState Weather { get; set; } = new();
    public GestureSignature Signature { get; set; } = new();
    public List<MemoryEcho> WitnessedEchoes { get; set; } = [];
    public HashSet<string> DiscoveredRituals { get; set; } = [];
    public SanctuaryConstellations Constellations { get; set; } = new();
    public List<AscensionEvent> AscensionHistory { get; set; } = [];
    public int TotalDuetCasts { get; set; }
    public float SessionMusicalHarmony { get; set; }

    // Personal history
    public string? FirstBuildingName { get; set; }
    public Guid? FirstBuildingId { get; set; }
    public string? FirstFamiliarName { get; set; }
    public Guid? FirstFamiliarId { get; set; }
    public int FirstSpellCastCount { get; set; }
}

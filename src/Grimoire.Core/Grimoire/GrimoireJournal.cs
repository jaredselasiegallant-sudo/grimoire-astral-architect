using Grimoire.Core.Enums;
using Grimoire.Core.Models;

namespace Grimoire.Core.Grimoire;

/// <summary>
/// The living Grimoire journal — a codex that fills in as the player
/// discovers recipes, familiar species, buildings, and lore.
/// Each entry is hand-written style text that unlocks progressively.
/// </summary>
public sealed class GrimoireJournal
{
    /// <summary>All known entries, keyed by entry ID.</summary>
    public Dictionary<string, GrimoireEntry> Entries { get; set; } = [];

    /// <summary>Number of total entries in the full codex.</summary>
    public int TotalEntries => Enum.GetNames<GrmoireEntryId>().Length;

    /// <summary>Number of unlocked entries.</summary>
    public int UnlockedCount => Entries.Count(e => e.Value.IsUnlocked);

    /// <summary>Completion percentage.</summary>
    public double CompletionPercentage => TotalEntries == 0 ? 0 : (double)UnlockedCount / TotalEntries * 100;

    /// <summary>Unlock a specific entry. Returns true if newly unlocked.</summary>
    public bool Unlock(GrmoireEntryId entryId)
    {
        var id = entryId.ToString();
        if (Entries.TryGetValue(id, out var existing))
        {
            if (existing.IsUnlocked) return false;
            existing.IsUnlocked = true;
            existing.UnlockedUTC = DateTimeOffset.UtcNow;
            return true;
        }

        Entries[id] = new GrimoireEntry
        {
            Id = id,
            IsUnlocked = true,
            UnlockedUTC = DateTimeOffset.UtcNow
        };
        return true;
    }

    /// <summary>Check if a specific entry is unlocked.</summary>
    public bool IsUnlocked(GrmoireEntryId entryId) =>
        Entries.TryGetValue(entryId.ToString(), out var e) && e.IsUnlocked;

    /// <summary>Get all entries of a specific category.</summary>
    public List<GrimoireEntry> GetEntries(GrmoireCategory category) =>
        Entries.Values.Where(e => e.Category == category).OrderBy(e => e.SortOrder).ToList();

    /// <summary>Get total entries per category for progress display.</summary>
    public Dictionary<GrmoireCategory, (int unlocked, int total)> GetCategoryProgress()
    {
        var allEntries = GetAllEntryMetadata();
        return Enum.GetValues<GrmoireCategory>()
            .ToDictionary(
                cat => cat,
                cat => (
                    unlocked: allEntries.Count(e => e.Category == cat && Entries.ContainsKey(e.Id) && Entries[e.Id].IsUnlocked),
                    total: allEntries.Count(e => e.Category == cat)
                )
            );
    }

    /// <summary>Get the full list of possible entries (metadata for locked ones).</summary>
    public static List<GrimoireEntryMetadata> GetAllEntryMetadata() =>
    [
        // Familiars
        new("fam_wisp", "Ember Wisp", "A spirit of living flame, gentle and curious.", GrmoireCategory.Familiars, 0),
        new("fam_sprite", "Mana Sprite", "Born from raw magical essence, always dancing.", GrmoireCategory.Familiars, 1),
        new("fam_drakling", "Void Drakling", "Small but fierce, drawn to the deep places.", GrmoireCategory.Familiars, 2),
        new("fam_mothwing", "Mothwing", "Soft-spoken and soothing, drawn to moonlight.", GrmoireCategory.Familiars, 3),
        new("fam_golem", "Stone Golem", "Ancient and steady, remembers the old ways.", GrmoireCategory.Familiars, 4),
        new("fam_shade", "Shade Walker", "Born from the Void, not evil — just forgotten.", GrmoireCategory.Familiars, 5),
        new("fam_foxfire", "Foxfire Spirit", "Tricksy and bright, leads you to hidden things.", GrmoireCategory.Familiars, 6),

        // Buildings
        new("bld_shrine", "Mana Shrine", "The heart of every sanctuary. Draws mana from the stars.", GrmoireCategory.Buildings, 10),
        new("bld_potion", "Potion Station", "Where raw wonder becomes something useful.", GrmoireCategory.Buildings, 11),
        new("bld_habitat", "Familiar Habitat", "A home within a home. Familiars thrive here.", GrmoireCategory.Buildings, 12),
        new("bld_cauldron", "Alchemical Cauldron", "The old art of combining essences.", GrmoireCategory.Buildings, 13),
        new("bld_obelisk", "Starlight Obelisk", "Passive mana regeneration from starlight.", GrmoireCategory.Buildings, 14),
        new("bld_anchor", "Void Anchor", "Stabilizes the sanctuary against Void corruption.", GrmoireCategory.Buildings, 15),
        new("bld_garden", "Garden of Whispers", "Where plants grow and secrets are told.", GrmoireCategory.Buildings, 16),

        // Recipes
        new("rec_clarity", "Clarity Draught", "Mana and Void in perfect tension.", GrmoireCategory.Recipes, 20),
        new("rec_ember_tea", "Ember Tea", "Warmth you can drink. Heals familiars.", GrmoireCategory.Recipes, 21),
        new("rec_frost_shard", "Frost Shard Crystal", "Frozen mana. Sharp and beautiful.", GrmoireCategory.Recipes, 22),
        new("rec_void_dust", "Void Dust", "The residue of forgotten things.", GrmoireCategory.Recipes, 23),
        new("rec_starlight_essence", "Starlight Essence", "Liquid starlight. Extremely rare.", GrmoireCategory.Recipes, 24),

        // Lore
        new("lore_falling", "The Falling", "When the sanctuaries fell dark, one by one.", GrmoireCategory.Lore, 30),
        new("lore_architects", "The Architects", "Those who built the floating gardens.", GrmoireCategory.Lore, 31),
        new("lore_sky_forgot", "The Sky Forgot", "A distant voice speaks of being forgotten.", GrmoireCategory.Lore, 32),
        new("lore_old_runes", "The Old Runes", "Shapes that hold power when drawn with intent.", GrmoireCategory.Lore, 33),
        new("lore_void_dust_fall", "Void Dust and the Fall", "What Void Dust actually is — ground-up sanctuary.", GrmoireCategory.Lore, 34)
    ];
}

public sealed class GrimoireEntry
{
    public required string Id { get; init; }
    public bool IsUnlocked { get; set; }
    public DateTimeOffset? UnlockedUTC { get; set; }
    public GrmoireCategory Category { get; init; }
    public int SortOrder { get; init; }
}

public sealed record GrimoireEntryMetadata(
    string Id,
    string Title,
    string Description,
    GrmoireCategory Category,
    int SortOrder
);

public enum GrmoireEntryId
{
    // Familiars
    fam_wisp, fam_sprite, fam_drakling, fam_mothwing, fam_golem, fam_shade, fam_foxfire,
    // Buildings
    bld_shrine, bld_potion, bld_habitat, bld_cauldron, bld_obelisk, bld_anchor, bld_garden,
    // Recipes
    rec_clarity, rec_ember_tea, rec_frost_shard, rec_void_dust, rec_starlight_essence,
    // Lore
    lore_falling, lore_architects, lore_sky_forgot, lore_old_runes, lore_void_dust_fall
}

public enum GrmoireCategory
{
    Familiars,
    Buildings,
    Recipes,
    Lore
}

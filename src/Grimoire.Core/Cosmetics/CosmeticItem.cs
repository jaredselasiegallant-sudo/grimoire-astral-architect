using Grimoire.Core.Enums;

namespace Grimoire.Core.Cosmetics;

/// <summary>
/// A cosmetic customization item: shrine skin, particle colour, familiar accessory.
/// Purely visual — no gameplay effect.
/// </summary>
public sealed class CosmeticItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required string Description { get; init; }
    public CosmeticType Type { get; init; }

    /// <summary>Hex colour string for tinting (e.g. "#FF6AAE").</summary>
    public string? ColourHex { get; init; }

    /// <summary>Texture path or particle effect ID.</summary>
    public string? EffectId { get; init; }

    /// <summary>Whether the player has unlocked this cosmetic.</summary>
    public bool IsUnlocked { get; set; }

    /// <summary>Whether this cosmetic is currently equipped.</summary>
    public bool IsEquipped { get; set; }

    /// <summary>How to unlock: achievement, discovery, purchase, bond level.</summary>
    public string? UnlockRequirement { get; init; }
}

/// <summary>Types of cosmetic items.</summary>
public enum CosmeticType
{
    ShrineSkin,
    ParticleColour,
    FamiliarAccessory,
    SkyboxTint,
    GestureTrailStyle,
    SanctuaryTheme
}

/// <summary>
/// Player's active cosmetic loadout.
/// </summary>
public sealed class CosmeticLoadout
{
    public string? ActiveShrineSkin { get; set; }
    public string? ActiveParticleColour { get; set; }
    public string? ActiveFamiliarAccessory { get; set; }
    public string? ActiveSkyboxTint { get; set; }
    public string? ActiveTrailStyle { get; set; }
    public string? ActiveSanctuaryTheme { get; set; }
}

/// <summary>
/// Default cosmetics available from the start.
/// </summary>
public static class DefaultCosmetics
{
    public static List<CosmeticItem> GetAll() =>
    [
        new() { Name = "Default Shrine", Description = "The original shrine aesthetic.", Type = CosmeticType.ShrineSkin, ColourHex = "#64C8FF", IsUnlocked = true, IsEquipped = true },
        new() { Name = "Cyan Glow", Description = "Classic cyan particle effects.", Type = CosmeticType.ParticleColour, ColourHex = "#64C8FF", IsUnlocked = true, IsEquipped = true },
        new() { Name = "Silver Trail", Description = "A clean, silver gesture trail.", Type = CosmeticType.GestureTrailStyle, ColourHex = "#C0C0C0", IsUnlocked = true, IsEquipped = true },

        // Unlockable cosmetics
        new() { Name = "Void Shrine", Description = "Dark shrine with purple glow.", Type = CosmeticType.ShrineSkin, ColourHex = "#A064FF", UnlockRequirement = "Discover Void Dust recipe" },
        new() { Name = "Ember Particles", Description = "Warm, fiery particle effects.", Type = CosmeticType.ParticleColour, ColourHex = "#FF6A3D", UnlockRequirement = "Reach Bond Level 5 with any familiar" },
        new() { Name = "Golden Trail", Description = "A radiant golden gesture trail.", Type = CosmeticType.GestureTrailStyle, ColourHex = "#FFE864", UnlockRequirement = "Complete 10 expeditions" },
        new() { Name = "Frost Shrine", Description = "Crystalline ice shrine.", Type = CosmeticType.ShrineSkin, ColourHex = "#7DD4FF", UnlockRequirement = "Discover Frost Shard Crystal recipe" },
        new() { Name = "Verdant Theme", Description = "Lush green sanctuary palette.", Type = CosmeticType.SanctuaryTheme, ColourHex = "#64FF8A", UnlockRequirement = "Build a Garden of Whispers" },
        new() { Name = "Starlight Trail", Description = "Trail that sparkles like stars.", Type = CosmeticType.GestureTrailStyle, ColourHex = "#FFE864", UnlockRequirement = "Reach Grimoire 50% completion" },
        new() { Name = "Cosmic Shrine", Description = "Shifting rainbow shrine.", Type = CosmeticType.ShrineSkin, ColourHex = "#FF6AAE", UnlockRequirement = "Witness a Cosmic Alignment event" },
    ];
}

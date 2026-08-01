namespace Grimoire.Core.Enums;

/// <summary>Spell combo tiers based on gesture chain speed and precision.</summary>
public enum ComboTier
{
    None,
    Basic,      // Single gesture
    Chained,    // 2 gestures within 3 seconds
    Ascended,   // 3 gestures within 5 seconds
    Transcendent // 4+ gestures within 8 seconds
}

/// <summary>Archetype a player-drawn spell maps to.</summary>
public enum SpellArchetype
{
    Ward,       // Defensive, protective, enclosing
    Bolt,       // Offensive, directional, fast
    Heal,       // Restorative, soothing
    Summon,     // Calls forth entities or objects
    Enchant,    // Buffs, enhances, transforms
    Dispel      // Removes, cleanses, undoes
}

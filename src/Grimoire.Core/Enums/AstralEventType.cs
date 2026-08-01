namespace Grimoire.Core.Enums;

/// <summary>Types of Astral Events that rotate on daily/weekly schedules.</summary>
public enum AstralEventType
{
    ManaRift,          // Bonus mana from all buildings for 24h
    VoidComet,         // Rare Void Dust drops for 12h
    StarfallShower,    // Experience multiplier for familiars for 6h
    LuminousConfluence,// Light element crafting bonus for 24h
    EmberWhirlwind,    // Ember element expedition bonus for 12h
    FrostHarvest,      // Frost element drops doubled for 24h
    VerdantBloom,      // Garden yields tripled for 24h
    UmbralVeil,        // Shadow element discovery chance +50% for 12h
    CosmicAlignment    // All bonuses at half strength for 48h (weekly rare)
}

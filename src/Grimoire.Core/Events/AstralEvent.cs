using Grimoire.Core.Enums;

namespace Grimoire.Core.Events;

/// <summary>
/// A time-limited Astral Event tied to real-world calendar.
/// Rotates daily/weekly, providing unique bonuses and visual changes.
/// </summary>
public sealed class AstralEvent
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public AstralEventType Type { get; init; }
    public EventTypeFrequency Frequency { get; init; }

    /// <summary>Duration of the event in hours.</summary>
    public int DurationHours { get; init; }

    /// <summary>UTC start time of this occurrence.</summary>
    public DateTimeOffset StartUTC { get; init; }

    /// <summary>UTC end time.</summary>
    public DateTimeOffset EndUTC { get; init; }

    /// <summary>Whether this event is currently active.</summary>
    public bool IsActive => DateTimeOffset.UtcNow >= StartUTC && DateTimeOffset.UtcNow < EndUTC;

    /// <summary>Multipliers applied while this event is active.</summary>
    public Dictionary<string, double> Multipliers { get; init; } = [];

    /// <summary>Visual colour tint applied to the skybox during event.</summary>
    public string SkyTintHex { get; init; } = "#FFFFFF";
}

public enum EventTypeFrequency
{
    Daily,
    Weekly,
    Seasonal,
    Rare
}

/// <summary>
/// Generates Astral Events based on the current real-world date.
/// Deterministic: same date always produces the same events.
/// </summary>
public static class AstralEventScheduler
{
    private static readonly (string name, string desc, AstralEventType type, int hours, string tint, EventTypeFrequency freq)[] EventTemplates =
    [
        ("Mana Rift", "A rift in the fabric of space draws mana from distant stars.", AstralEventType.ManaRift, 24, "#64C8FF", EventTypeFrequency.Daily),
        ("Void Comet", "A comet streaks through the Void, scattering rare dust.", AstralEventType.VoidComet, 12, "#A064FF", EventTypeFrequency.Daily),
        ("Starfall Shower", "Falling stars grant your familiars radiant experience.", AstralEventType.StarfallShower, 6, "#FFE864", EventTypeFrequency.Daily),
        ("Luminous Confluence", "Light energies converge, enhancing alchemical potency.", AstralEventType.LuminousConfluence, 24, "#FFFFFF", EventTypeFrequency.Weekly),
        ("Ember Whirlwind", "Fires of creation swirl through the sanctuary.", AstralEventType.EmberWhirlwind, 12, "#FF6A3D", EventTypeFrequency.Daily),
        ("Frost Harvest", "Crystalline energies gather in the cold hours.", AstralEventType.FrostHarvest, 24, "#7DD4FF", EventTypeFrequency.Daily),
        ("Verdant Bloom", "Life surges through every growing thing.", AstralEventType.VerdantBloom, 24, "#64FF8A", EventTypeFrequency.Weekly),
        ("Umbral Veil", "The shadows part, revealing hidden secrets.", AstralEventType.UmbralVeil, 12, "#6A3DFF", EventTypeFrequency.Daily),
        ("Cosmic Alignment", "All celestial forces align in rare harmony.", AstralEventType.CosmicAlignment, 48, "#FFE864", EventTypeFrequency.Rare)
    ];

    /// <summary>Generate events for today based on the date seed.</summary>
    public static List<AstralEvent> GetTodaysEvents()
    {
        var today = DateTimeOffset.UtcNow.Date;
        var events = new List<AstralEvent>();

        // Daily events: 2 selected by date hash
        var dayHash = today.GetHashCode();
        var daily1 = EventTemplates[dayHash % 5]; // First 5 are daily
        var daily2 = EventTemplates[(dayHash / 5 + 1) % 5];

        events.Add(CreateEvent(daily1, today, EventTypeFrequency.Daily));

        // Second daily only if hash warrants it (roughly 60% chance)
        if (dayHash % 10 < 6)
            events.Add(CreateEvent(daily2, today.AddHours(6), EventTypeFrequency.Daily));

        // Weekly event: one per week, selected by week number
        var weekNumber = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
            today, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        var weeklyTemplate = EventTemplates[5 + (weekNumber % 4)];
        events.Add(CreateEvent(weeklyTemplate, today.AddDays(-(int)today.DayOfWeek), EventTypeFrequency.Weekly));

        // Rare event: roughly once per month (day 15-ish)
        if (today.Day >= 14 && today.Day <= 16)
        {
            var rareTemplate = EventTemplates[^1]; // Cosmic Alignment
            events.Add(CreateEvent(rareTemplate, today, EventTypeFrequency.Rare));
        }

        return events;
    }

    private static AstralEvent CreateEvent(
        (string name, string desc, AstralEventType type, int hours, string tint, EventTypeFrequency freq) template,
        DateTimeOffset start,
        EventTypeFrequency freq)
    {
        return new AstralEvent
        {
            Id = $"{template.type}_{start:yyyyMMdd}",
            Name = template.name,
            Description = template.desc,
            Type = template.type,
            Frequency = template.freq,
            DurationHours = template.hours,
            StartUTC = start,
            EndUTC = start.AddHours(template.hours),
            SkyTintHex = template.tint,
            Multipliers = template.type switch
            {
                AstralEventType.ManaRift => new() { ["ManaPerSecond"] = 2.0 },
                AstralEventType.VoidComet => new() { ["VoidDropRate"] = 3.0 },
                AstralEventType.StarfallShower => new() { ["XpMultiplier"] = 2.5 },
                AstralEventType.LuminousConfluence => new() { ["CraftingPower"] = 1.5 },
                AstralEventType.EmberWhirlwind => new() { ["EmberExpeditionBonus"] = 2.0 },
                AstralEventType.FrostHarvest => new() { ["FrostDropRate"] = 2.0 },
                AstralEventType.VerdantBloom => new() { ["GardenYield"] = 3.0 },
                AstralEventType.UmbralVeil => new() { ["DiscoveryChance"] = 1.5 },
                AstralEventType.CosmicAlignment => new() { ["AllBonus"] = 1.5 },
                _ => new()
            }
        };
    }
}

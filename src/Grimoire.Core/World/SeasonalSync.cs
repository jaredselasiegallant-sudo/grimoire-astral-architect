using Grimoire.Core.Enums;

namespace Grimoire.Core.World;

/// <summary>
/// Syncs the game world to the real-world season and date.
/// Provides palette shifts, event triggers, and ambient changes.
/// </summary>
public static class SeasonalSync
{
    /// <summary>Get the current season from the real-world date.</summary>
    public static Season GetCurrentSeason() => DateTimeOffset.UtcNow.Month switch
    {
        3 or 4 or 5 => Season.Spring,
        6 or 7 or 8 => Season.Summer,
        9 or 10 or 11 => Season.Autumn,
        _ => Season.Winter
    };

    /// <summary>Get the current time of day zone.</summary>
    public static TimeOfDay GetCurrentTimeOfDay()
    {
        var hour = DateTime.Now.Hour;
        return hour switch
        {
            < 4 => TimeOfDay.DeepNight,
            < 7 => TimeOfDay.Dawn,
            < 12 => TimeOfDay.Morning,
            < 17 => TimeOfDay.Afternoon,
            < 19 => TimeOfDay.Dusk,
            _ => TimeOfDay.Night
        };
    }

    /// <summary>Get the skybox palette for the current season + time of day.</summary>
    public static SeasonalPalette GetCurrentPalette()
    {
        var season = GetCurrentSeason();
        var time = GetCurrentTimeOfDay();
        return GetPalette(season, time);
    }

    /// <summary>Get a specific palette. Deterministic for testing.</summary>
    public static SeasonalPalette GetPalette(Season season, TimeOfDay time)
    {
        return (season, time) switch
        {
            // Spring — soft greens and pinks
            (Season.Spring, TimeOfDay.DeepNight) => new("Spring Night", "#0A1A0A", "#1A2A1A", "#FFB0D0", 0.3f),
            (Season.Spring, TimeOfDay.Dawn) => new("Spring Dawn", "#2A3A2A", "#FFB0D0", "#FFE0F0", 0.6f),
            (Season.Spring, TimeOfDay.Morning) => new("Spring Morning", "#E0FFE0", "#B0FFB0", "#FFFFFF", 0.9f),
            (Season.Spring, TimeOfDay.Afternoon) => new("Spring Afternoon", "#C0F0C0", "#90E090", "#FFFFFF", 0.85f),
            (Season.Spring, TimeOfDay.Dusk) => new("Spring Dusk", "#3A2A3A", "#FFB0D0", "#FF80A0", 0.5f),
            (Season.Spring, TimeOfDay.Night) => new("Spring Night", "#0A1A0A", "#1A2A1A", "#FFB0D0", 0.3f),

            // Summer — warm golds and bright blues
            (Season.Summer, TimeOfDay.DeepNight) => new("Summer Night", "#0A0A1A", "#1A1A2A", "#FFD080", 0.4f),
            (Season.Summer, TimeOfDay.Dawn) => new("Summer Dawn", "#2A2A1A", "#FFD080", "#FFE8A0", 0.7f),
            (Season.Summer, TimeOfDay.Morning) => new("Summer Morning", "#F0F0E0", "#FFE8A0", "#FFFFFF", 1.0f),
            (Season.Summer, TimeOfDay.Afternoon) => new("Summer Afternoon", "#F0F0C0", "#FFE060", "#FFFFFF", 0.95f),
            (Season.Summer, TimeOfDay.Dusk) => new("Summer Dusk", "#2A1A1A", "#FF8040", "#FFB060", 0.6f),
            (Season.Summer, TimeOfDay.Night) => new("Summer Night", "#0A0A1A", "#1A1A2A", "#FFD080", 0.4f),

            // Autumn — deep oranges and warm browns
            (Season.Autumn, TimeOfDay.DeepNight) => new("Autumn Night", "#1A0A0A", "#2A1A1A", "#FFA060", 0.35f),
            (Season.Autumn, TimeOfDay.Dawn) => new("Autumn Dawn", "#2A1A0A", "#FFA060", "#FFC080", 0.65f),
            (Season.Autumn, TimeOfDay.Morning) => new("Autumn Morning", "#F0E0C0", "#FFB070", "#FFFFFF", 0.85f),
            (Season.Autumn, TimeOfDay.Afternoon) => new("Autumn Afternoon", "#E0D0B0", "#FF9040", "#FFFFFF", 0.8f),
            (Season.Autumn, TimeOfDay.Dusk) => new("Autumn Dusk", "#2A1A0A", "#FF7020", "#FFA050", 0.55f),
            (Season.Autumn, TimeOfDay.Night) => new("Autumn Night", "#1A0A0A", "#2A1A1A", "#FFA060", 0.35f),

            // Winter — cool blues and soft whites
            (Season.Winter, TimeOfDay.DeepNight) => new("Winter Night", "#0A0A2A", "#1A1A3A", "#A0C0FF", 0.25f),
            (Season.Winter, TimeOfDay.Dawn) => new("Winter Dawn", "#1A1A2A", "#A0C0FF", "#C0E0FF", 0.55f),
            (Season.Winter, TimeOfDay.Morning) => new("Winter Morning", "#E0F0FF", "#C0E0FF", "#FFFFFF", 0.9f),
            (Season.Winter, TimeOfDay.Afternoon) => new("Winter Afternoon", "#D0E8FF", "#A0D0FF", "#FFFFFF", 0.85f),
            (Season.Winter, TimeOfDay.Dusk) => new("Winter Dusk", "#1A1A3A", "#80A0E0", "#A0C0FF", 0.5f),
            (Season.Winter, TimeOfDay.Night) => new("Winter Night", "#0A0A2A", "#1A1A3A", "#A0C0FF", 0.25f),

            _ => new("Default", "#0A0A1A", "#1A1A2A", "#FFFFFF", 0.5f)
        };
    }
}

public sealed record SeasonalPalette(
    string Name,
    string SkyTopHex,
    string SkyBottomHex,
    string AccentHex,
    float Brightness
);

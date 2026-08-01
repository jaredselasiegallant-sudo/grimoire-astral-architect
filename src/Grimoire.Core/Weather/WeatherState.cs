using Grimoire.Core.Enums;

namespace Grimoire.Core.Weather;

/// <summary>
/// Weather system with gameplay consequences, not just visual mood.
/// Weather conditions affect crafting output, familiar behavior,
/// hatch rates, and expedition rewards.
/// </summary>
public sealed class WeatherState
{
    public WeatherType CurrentWeather { get; set; } = WeatherType.Clear;
    public float Intensity { get; set; } = 1.0f;
    public DateTimeOffset StartedUTC { get; set; } = DateTimeOffset.UtcNow;
    public TimeSpan Duration { get; set; } = TimeSpan.FromHours(4);

    /// <summary>Whether the weather is currently active.</summary>
    public bool IsActive => DateTimeOffset.UtcNow < StartedUTC + Duration;

    /// <summary>Time remaining in this weather event.</summary>
    public TimeSpan TimeRemaining
    {
        get
        {
            var remaining = (StartedUTC + Duration) - DateTimeOffset.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    /// <summary>Get the gameplay modifiers applied by this weather.</summary>
    public WeatherEffects GetEffects() => CurrentWeather switch
    {
        WeatherType.Starstorm => new WeatherEffects
        {
            CauldronOutputMultiplier = 1.5f,
            ItemScatterChance = 0.2f,
            FamiliarGatheringMultiplier = 1.3f,
            HatchSpeedMultiplier = 1.0f,
            VisibilityModifier = 0.7f,
            Description = "The Cauldron surcharges with starlight, but loose items may scatter."
        },
        WeatherType.Fog => new WeatherEffects
        {
            CauldronOutputMultiplier = 0.8f,
            ItemScatterChance = 0.0f,
            FamiliarGatheringMultiplier = 0.6f,
            HatchSpeedMultiplier = 1.8f, // rare nocturnal species hatch faster
            NocturnalHatchBonus = true,
            VisibilityModifier = 0.4f,
            Description = "Fog slows familiar movement, but rare nocturnal species stir."
        },
        WeatherType.ManaRain => new WeatherEffects
        {
            CauldronOutputMultiplier = 1.2f,
            ManaRegenMultiplier = 2.0f,
            FamiliarGatheringMultiplier = 1.1f,
            HatchSpeedMultiplier = 1.0f,
            VisibilityModifier = 0.9f,
            Description = "Mana crystallizes from the rain, saturating the sanctuary."
        },
        WeatherType.VoidBreach => new WeatherEffects
        {
            CauldronOutputMultiplier = 0.5f,
            CorruptionMultiplier = 1.5f,
            FamiliarGatheringMultiplier = 0.7f,
            HatchSpeedMultiplier = 0.5f,
            VisibilityModifier = 0.6f,
            Description = "The Void presses in. Builds corruption faster, but rare Void resources appear."
        },
        WeatherType.Aurora => new WeatherEffects
        {
            CauldronOutputMultiplier = 1.3f,
            XpMultiplier = 1.5f,
            FamiliarGatheringMultiplier = 1.2f,
            HatchSpeedMultiplier = 1.2f,
            VisibilityModifier = 1.2f,
            Description = "The aurora bathes everything in shifting light. Familiars flourish."
        },
        WeatherType.Calm => new WeatherEffects
        {
            CauldronOutputMultiplier = 1.0f,
            FamiliarGatheringMultiplier = 1.0f,
            HatchSpeedMultiplier = 1.0f,
            VisibilityModifier = 1.0f,
            Description = "A quiet day. Nothing特别, nothing dangerous."
        },
        _ => new WeatherEffects { Description = "Clear skies." }
    };
}

public enum WeatherType
{
    Clear,
    Calm,
    Starstorm,
    Fog,
    ManaRain,
    VoidBreach,
    Aurora
}

public sealed class WeatherEffects
{
    public float CauldronOutputMultiplier { get; init; } = 1.0f;
    public float ManaRegenMultiplier { get; init; } = 1.0f;
    public float FamiliarGatheringMultiplier { get; init; } = 1.0f;
    public float HatchSpeedMultiplier { get; init; } = 1.0f;
    public float XpMultiplier { get; init; } = 1.0f;
    public float CorruptionMultiplier { get; init; } = 1.0f;
    public float VisibilityModifier { get; init; } = 1.0f;
    public float ItemScatterChance { get; init; }
    public bool NocturnalHatchBonus { get; init; }
    public required string Description { get; init; }
}

/// <summary>
/// Generates weather based on real-world time, season, and game events.
/// </summary>
public static class WeatherGenerator
{
    private static readonly WeatherType[] SeasonalWeights_Spring = [WeatherType.Calm, WeatherType.ManaRain, WeatherType.Aurora, WeatherType.Fog];
    private static readonly WeatherType[] SeasonalWeights_Summer = [WeatherType.Calm, WeatherType.Starstorm, WeatherType.Aurora, WeatherType.Calm];
    private static readonly WeatherType[] SeasonalWeights_Autumn = [WeatherType.Fog, WeatherType.VoidBreach, WeatherType.Calm, WeatherType.Starstorm];
    private static readonly WeatherType[] SeasonalWeights_Winter = [WeatherType.Starstorm, WeatherType.Fog, WeatherType.VoidBreach, WeatherType.Calm];

    public static WeatherState Generate(DateTimeOffset now, Season season)
    {
        var pool = season switch
        {
            Season.Spring => SeasonalWeights_Spring,
            Season.Summer => SeasonalWeights_Summer,
            Season.Autumn => SeasonalWeights_Autumn,
            Season.Winter => SeasonalWeights_Winter,
            _ => SeasonalWeights_Spring
        };

        var rng = new Random(now.Hour * 31 + now.Day); // Deterministic per day-hour
        var type = pool[rng.Next(pool.Length)];

        // Intensity varies by time of day
        var intensity = type switch
        {
            WeatherType.Starstorm => 0.8f + (float)rng.NextDouble() * 0.4f,
            WeatherType.Fog => 0.5f + (float)rng.NextDouble() * 0.5f,
            WeatherType.VoidBreach => 0.6f + (float)rng.NextDouble() * 0.4f,
            _ => 1.0f
        };

        // Duration: 2-6 hours depending on severity
        var hours = type switch
        {
            WeatherType.Starstorm => 2 + rng.Next(3),
            WeatherType.VoidBreach => 2 + rng.Next(2),
            WeatherType.Aurora => 3 + rng.Next(4),
            _ => 4 + rng.Next(3)
        };

        // Start time aligned to current hour
        var startHour = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset);

        return new WeatherState
        {
            CurrentWeather = type,
            Intensity = intensity,
            StartedUTC = startHour,
            Duration = TimeSpan.FromHours(hours)
        };
    }
}

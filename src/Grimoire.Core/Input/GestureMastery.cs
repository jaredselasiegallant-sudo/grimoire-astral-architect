using Grimoire.Core.Enums;

namespace Grimoire.Core.Input;

/// <summary>
/// Gesture Mastery — tracks quality of each gesture attempt.
/// "The Shape of Intent" — quality matters as much as getting the shape right.
/// A sloppy circle still summons mana, but a precise one is far more potent.
/// </summary>
public sealed class GestureMastery
{
    private readonly Dictionary<SpellGesture, GestureQualityRecord> _records = [];

    /// <summary>
    /// Get mastery level for a gesture (0-100).
    /// </summary>
    public int GetMasteryLevel(SpellGesture gesture)
    {
        if (!_records.TryGetValue(gesture, out var record)) return 0;
        return Math.Clamp((int)(record.AverageQuality * 100), 0, 100);
    }

    /// <summary>
    /// Get mastery title for display.
    /// </summary>
    public string GetMasteryTitle(SpellGesture gesture) => GetMasteryLevel(gesture) switch
    {
        >= 95 => "Transcendent",
        >= 80 => "Masterful",
        >= 60 => "Skilled",
        >= 40 => "Apprentice",
        >= 20 => "Novice",
        _ => "Uninitiated"
    };

    /// <summary>
    /// Record a gesture attempt and return its quality assessment.
    /// </summary>
    public GestureQualityRecord RecordAttempt(SpellGesture gesture, float[] deviations, float speed, float duration)
    {
        var quality = CalculateQuality(deviations, speed, duration);
        var tier = GetQualityTier(quality);

        if (!_records.TryGetValue(gesture, out var existing))
        {
            existing = new GestureQualityRecord { Gesture = gesture };
            _records[gesture] = existing;
        }

        existing.TotalAttempts++;
        existing.TotalQuality += quality;
        existing.AverageQuality = existing.TotalQuality / existing.TotalAttempts;
        existing.BestQuality = Math.Max(existing.BestQuality, quality);
        existing.LastAttemptUTC = DateTimeOffset.UtcNow;

        return new GestureQualityRecord
        {
            Gesture = gesture,
            Quality = quality,
            Tier = tier,
            TotalAttempts = existing.TotalAttempts,
            AverageQuality = existing.AverageQuality
        };
    }

    /// <summary>
    /// Get potency multiplier based on gesture quality.
    /// 0.5x at worst, 2.0x at best.
    /// </summary>
    public static float GetPotencyMultiplier(float quality) =>
        0.5f + (quality * 1.5f);

    /// <summary>
    /// Get duration multiplier based on gesture quality.
    /// Effects last longer when cast with precision.
    /// </summary>
    public static float GetDurationMultiplier(float quality) =>
        0.7f + (quality * 0.6f);

    /// <summary>
    /// Check if a gesture quality unlocks bonus effects.
    /// </summary>
    public static bool UnlocksBonusEffect(float quality) => quality >= 0.85f;

    private static float CalculateQuality(float[] deviations, float speed, float duration)
    {
        if (deviations.Length == 0) return 0.5f;

        // Shape accuracy (lower deviation = better)
        float avgDeviation = deviations.Average();
        float shapeScore = Math.Clamp(1.0f - (avgDeviation / 50f), 0f, 1f);

        // Speed consistency (not too fast, not too slow)
        float speedScore = speed switch
        {
            < 0.3f => 0.4f,  // Too slow
            < 0.8f => 0.7f,  // Slightly slow
            < 1.5f => 1.0f,  // Ideal speed
            < 2.5f => 0.8f,  // Slightly fast
            _ => 0.5f         // Too fast
        };

        // Duration appropriateness
        float durationScore = duration switch
        {
            < 0.3f => 0.3f,   // Too brief
            < 0.8f => 0.6f,   // A bit quick
            < 2.0f => 1.0f,   // Good
            < 4.0f => 0.8f,   // A bit slow
            _ => 0.5f          // Too slow
        };

        // Weighted average: shape matters most
        return (shapeScore * 0.5f) + (speedScore * 0.25f) + (durationScore * 0.25f);
    }

    private static GestureQualityTier GetQualityTier(float quality) => quality switch
    {
        >= 0.9f => GestureQualityTier.Transcendent,
        >= 0.75f => GestureQualityTier.Masterful,
        >= 0.55f => GestureQualityTier.Good,
        >= 0.35f => GestureQualityTier.Fair,
        _ => GestureQualityTier.Poor
    };

    /// <summary>
    /// Serialise all mastery data for persistence.
    /// </summary>
    public Dictionary<string, float> Serialise() =>
        _records.ToDictionary(
            kvp => kvp.Key.ToString(),
            kvp => kvp.Value.AverageQuality);

    /// <summary>
    /// Deserialise mastery data from persistence.
    /// </summary>
    public static GestureMastery Deserialise(Dictionary<string, float> data)
    {
        var mastery = new GestureMastery();
        foreach (var kvp in data)
        {
            if (Enum.TryParse<SpellGesture>(kvp.Key, out var gesture))
            {
                mastery._records[gesture] = new GestureQualityRecord
                {
                    Gesture = gesture,
                    AverageQuality = kvp.Value,
                    BestQuality = kvp.Value
                };
            }
        }
        return mastery;
    }
}

public sealed class GestureQualityRecord
{
    public SpellGesture Gesture { get; init; }
    public float Quality { get; set; }
    public GestureQualityTier Tier { get; set; }
    public int TotalAttempts { get; set; }
    public float TotalQuality { get; set; }
    public float AverageQuality { get; set; }
    public float BestQuality { get; set; }
    public DateTimeOffset LastAttemptUTC { get; set; }
}

public enum GestureQualityTier
{
    Poor,
    Fair,
    Good,
    Masterful,
    Transcendent
}

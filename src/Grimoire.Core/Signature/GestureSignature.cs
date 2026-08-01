namespace Grimoire.Core.Signature;

/// <summary>
/// "The Sky Listens" — builds a subtle profile of how the player traces gestures.
/// Over time, the game learns the player's personal shape rhythm:
/// their typical speed, pressure patterns, angular preferences, and timing cadence.
/// 
/// Late-game "Architect's spells" only unlock when the game recognizes
/// the player's own gesture signature — meaning magic responds to nobody's hands but theirs.
/// 
/// Framed narratively: "the sky has learned your hand."
/// </summary>
public sealed class GestureSignature
{
    /// <summary>Number of gestures recorded for the signature profile.</summary>
    public int SampleCount { get; set; }

    /// <summary>Average drawing speed (points per second) across all recorded gestures.</summary>
    public float AverageSpeed { get; set; }

    /// <summary>Typical angular velocity (radians per second) — how quickly they change direction.</summary>
    public float AverageAngularVelocity { get; set; }

    /// <summary>Preferred starting direction (normalized angle 0-2PI).</summary>
    public float PreferredStartAngle { get; set; }

    /// <summary>Typical gesture duration in seconds.</summary>
    public float AverageDuration { get; set; }

    /// <summary>Confidence score (0-1) — how well we know this player's signature.</summary>
    public float Confidence => Math.Min(1.0f, SampleCount / 50f);

    /// <summary>Whether the player has unlocked Architect's Spells.</summary>
    public bool IsArchitectUnlocked => Confidence >= 0.8f;

    /// <summary>Personal cadence: average pause between consecutive gestures.</summary>
    public float AveragePauseBetweenGestures { get; set; }

    /// <summary>Shape preferences: which gesture types they draw most often.</summary>
    public Dictionary<string, int> ShapeFrequency { get; set; } = [];

    /// <summary>Timing signature: a histogram of gesture durations binned into 0.5s windows.</summary>
    public float[] TimingHistogram { get; set; } = new float[10]; // 0-5 seconds in 0.5s bins

    /// <summary>
    /// Record a new gesture observation and update the signature profile.
    /// Uses exponential moving average for smooth adaptation.
    /// </summary>
    public void RecordGesture(GestureObservation observation)
    {
        SampleCount++;

        // Exponential moving average with learning rate
        var alpha = 1.0f / Math.Min(SampleCount, 50);

        AverageSpeed = Lerp(AverageSpeed, observation.Speed, alpha);
        AverageAngularVelocity = Lerp(AverageAngularVelocity, observation.AngularVelocity, alpha);
        AverageDuration = Lerp(AverageDuration, observation.DurationSeconds, alpha);
        AveragePauseBetweenGestures = Lerp(AveragePauseBetweenGestures, observation.PauseAfterSeconds, alpha);

        // Circular mean for angle
        PreferredStartAngle = CircularMean(PreferredStartAngle, observation.StartAngle, alpha);

        // Update shape frequency
        var shapeKey = observation.GestureType.ToString();
        ShapeFrequency.TryGetValue(shapeKey, out var count);
        ShapeFrequency[shapeKey] = count + 1;

        // Update timing histogram
        var bin = Math.Clamp((int)(observation.DurationSeconds / 0.5f), 0, 9);
        TimingHistogram[bin] = Lerp(TimingHistogram[bin], 1.0f, alpha);
    }

    /// <summary>
    /// Compare a new gesture against the stored signature.
    /// Returns a match score (0-1). High score means "this feels like the same hand."
    /// </summary>
    public float MatchScore(GestureObservation observation)
    {
        if (SampleCount < 10) return 0.5f; // Not enough data — neutral

        var speedDiff = Math.Abs(AverageSpeed - observation.Speed) / (AverageSpeed + 0.001f);
        var angleDiff = AngularDifference(PreferredStartAngle, observation.StartAngle) / MathF.PI;
        var durationDiff = Math.Abs(AverageDuration - observation.DurationSeconds) / (AverageDuration + 0.001f);
        var angularDiff = Math.Abs(AverageAngularVelocity - observation.AngularVelocity) / (AverageAngularVelocity + 0.001f);

        // Weighted similarity score
        var score = 1.0f - (
            speedDiff * 0.25f +
            angleDiff * 0.20f +
            durationDiff * 0.25f +
            angularDiff * 0.30f
        );

        return Math.Clamp(score, 0f, 1f);
    }

    /// <summary>
    /// Determine which Architect's Spell tier is available based on match quality.
    /// </summary>
    public ArchitectSpellTier GetArchitectTier(GestureObservation observation)
    {
        if (!IsArchitectUnlocked) return ArchitectSpellTier.None;

        var score = MatchScore(observation);
        return score switch
        {
            >= 0.90f => ArchitectSpellTier.Transcendent, // Perfect match — "The sky knows your hand"
            >= 0.80f => ArchitectSpellTier.Refined,      // Strong match — "The sky remembers"
            >= 0.70f => ArchitectSpellTier.Apprentice,    // Moderate match — "A familiar gesture"
            _ => ArchitectSpellTier.None
        };
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static float CircularMean(float a, float b, float t)
    {
        var diff = b - a;
        // Normalize to -PI..PI
        while (diff > MathF.PI) diff -= 2 * MathF.PI;
        while (diff < -MathF.PI) diff += 2 * MathF.PI;
        return (a + diff * t) % (2 * MathF.PI);
    }

    private static float AngularDifference(float a, float b)
    {
        var diff = Math.Abs(a - b);
        return Math.Min(diff, 2 * MathF.PI - diff);
    }
}

/// <summary>Observation of a single gesture for signature analysis.</summary>
public sealed class GestureObservation
{
    public string GestureType { get; init; } = "";
    public float Speed { get; init; }
    public float AngularVelocity { get; init; }
    public float StartAngle { get; init; }
    public float DurationSeconds { get; init; }
    public float PauseAfterSeconds { get; init; }
    public int PointCount { get; init; }
    public float Smoothness { get; init; }
}

public enum ArchitectSpellTier
{
    None,
    Apprentice,   // "A familiar gesture"
    Refined,      // "The sky remembers"
    Transcendent  // "The sky knows your hand"
}

using Grimoire.Core.Enums;

namespace Grimoire.Core.Audio;

/// <summary>
/// Each familiar species has a unique "voice" — an instrument-like sound
/// that forms part of the generative music system's duet/harmony layer.
/// </summary>
public static class FamiliarVoice
{
    /// <summary>
    /// Returns the voice parameters for a familiar type.
    /// BaseNote (Hz), Attack (0-1), Sustain (0-1), VibratoDepth (Hz), Timbre class.
    /// </summary>
    public static VoiceProfile GetVoice(FamiliarType type) => type switch
    {
        FamiliarType.Wisp => new VoiceProfile
        {
            BaseNoteHz = 523.25f, // C5
            Attack = 0.1f,
            Sustain = 0.7f,
            VibratoDepth = 2.0f,
            VibratoRate = 5.5f,
            Timbre = VoiceTimbre.Sine,
            Volume = 0.6f,
            Decay = 0.8f,
            Description = "A warm, pure tone like a singing bowl held close."
        },
        FamiliarType.Sprite => new VoiceProfile
        {
            BaseNoteHz = 783.99f, // G5
            Attack = 0.05f,
            Sustain = 0.5f,
            VibratoDepth = 4.0f,
            VibratoRate = 8.0f,
            Timbre = VoiceTimbre.Square,
            Volume = 0.5f,
            Decay = 0.4f,
            Description = "Bright, staccato chirps like a music box with wings."
        },
        FamiliarType.Drakling => new VoiceProfile
        {
            BaseNoteHz = 220.00f, // A3
            Attack = 0.3f,
            Sustain = 0.9f,
            VibratoDepth = 1.5f,
            VibratoRate = 4.0f,
            Timbre = VoiceTimbre.Sawtooth,
            Volume = 0.7f,
            Decay = 1.2f,
            Description = "A deep, resonant hum like a cello played by the earth itself."
        },
        FamiliarType.Mothwing => new VoiceProfile
        {
            BaseNoteHz = 659.25f, // E5
            Attack = 0.2f,
            Sustain = 0.6f,
            VibratoDepth = 3.0f,
            VibratoRate = 6.5f,
            Timbre = VoiceTimbre.Triangle,
            Volume = 0.4f,
            Decay = 0.6f,
            Description = "A soft, breathy whisper like wind through silk curtains."
        },
        FamiliarType.Golem => new VoiceProfile
        {
            BaseNoteHz = 146.83f, // D3
            Attack = 0.5f,
            Sustain = 0.8f,
            VibratoDepth = 0.5f,
            VibratoRate = 2.0f,
            Timbre = VoiceTimbre.Noise,
            Volume = 0.8f,
            Decay = 1.5f,
            Description = "A heavy, grounding tone like stone resonating in a cave."
        },
        FamiliarType.Shade => new VoiceProfile
        {
            BaseNoteHz = 440.00f, // A4
            Attack = 0.4f,
            Sustain = 0.4f,
            VibratoDepth = 5.0f,
            VibratoRate = 3.0f,
            Timbre = VoiceTimbre.Sine,
            Volume = 0.3f,
            Decay = 0.3f,
            Description = "An ethereal echo that seems to come from inside your own head."
        },
        FamiliarType.Foxfire => new VoiceProfile
        {
            BaseNoteHz = 369.99f, // F#4
            Attack = 0.1f,
            Sustain = 0.65f,
            VibratoDepth = 2.5f,
            VibratoRate = 7.0f,
            Timbre = VoiceTimbre.Triangle,
            Volume = 0.55f,
            Decay = 0.7f,
            Description = "A playful, bouncing melody like a fox trotting through autumn leaves."
        },
        _ => new VoiceProfile
        {
            BaseNoteHz = 440.00f,
            Attack = 0.2f,
            Sustain = 0.5f,
            VibratoDepth = 2.0f,
            VibratoRate = 5.0f,
            Timbre = VoiceTimbre.Sine,
            Volume = 0.5f,
            Decay = 0.5f,
            Description = "A simple, honest tone."
        }
    };

    /// <summary>
    /// Calculate harmony intervals when two familiars sing together.
    /// Returns the interval type and consonance score (0-1).
    /// </summary>
    public static HarmonyResult GetHarmony(FamiliarType a, FamiliarType b)
    {
        var va = GetVoice(a);
        var vb = GetVoice(b);

        float ratio = va.BaseNoteHz > vb.BaseNoteHz
            ? va.BaseNoteHz / vb.BaseNoteHz
            : vb.BaseNoteHz / va.BaseNoteHz;

        // Common intervals and their consonance
        return ratio switch
        {
            var r when MathF.Abs(r - 1.0f) < 0.02f => new HarmonyResult("Unison", 1.0f, "Perfectly aligned"),
            var r when MathF.Abs(r - 1.25f) < 0.05f => new HarmonyResult("Major Third", 0.85f, "Warm and bright"),
            var r when MathF.Abs(r - 1.5f) < 0.05f => new HarmonyResult("Perfect Fifth", 0.9f, "Strong and stable"),
            var r when MathF.Abs(r - 1.33f) < 0.05f => new HarmonyResult("Perfect Fourth", 0.8f, "Open and flowing"),
            var r when MathF.Abs(r - 1.67f) < 0.05f => new HarmonyResult("Major Sixth", 0.75f, "Sweet and gentle"),
            var r when MathF.Abs(r - 1.12f) < 0.05f => new HarmonyResult("Major Second", 0.4f, "Tense but interesting"),
            var r when MathF.Abs(r - 1.89f) < 0.05f => new HarmonyResult("Minor Seventh", 0.5f, "Bittersweet longing"),
            var r when MathF.Abs(r - 1.41f) < 0.05f => new HarmonyResult("Tritone", 0.2f, "Dramatic tension"),
            _ => new HarmonyResult("Unknown Interval", 0.3f, "A strange, otherworldly blend")
        };
    }
}

public sealed class VoiceProfile
{
    public float BaseNoteHz { get; init; }
    public float Attack { get; init; }
    public float Sustain { get; init; }
    public float VibratoDepth { get; init; }
    public float VibratoRate { get; init; }
    public VoiceTimbre Timbre { get; init; }
    public float Volume { get; init; }
    public float Decay { get; init; }
    public required string Description { get; init; }
}

public sealed class HarmonyResult
{
    public string IntervalName { get; init; } = "";
    public float ConsonanceScore { get; init; }
    public string Feeling { get; init; } = "";

    public HarmonyResult(string intervalName, float consonanceScore, string feeling)
    {
        IntervalName = intervalName;
        ConsonanceScore = consonanceScore;
        Feeling = feeling;
    }
}

public enum VoiceTimbre
{
    Sine,
    Square,
    Sawtooth,
    Triangle,
    Noise
}

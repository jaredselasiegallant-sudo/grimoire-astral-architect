using SkiaSharp;

namespace Grimoire.Engine.Audio;

/// <summary>
/// Ambient soundscape manager. Controls dynamic music layers
/// tied to skybox state (day/dusk/night) and seasonal palette.
/// 
/// Audio layers:
///   - Base drone (always playing, low volume)
///   - Time-of-day layer (morning birds, night crickets, etc.)
///   - Seasonal layer (wind, rain, leaves, etc.)
///   - Event layer (astral event sounds)
///   - Signature motif (the recurring musical phrase that evolves)
///   
/// Since we're targeting WinUI 3 + SkiaSharp without external audio libs,
/// this provides the structure and API. Actual audio playback would use
/// NAudio, Windows.Media.Playback, or Silk.NET.OpenAL in production.
/// </summary>
public sealed class AmbientSoundscape
{
    private float _masterVolume = 0.7f;
    private float _musicVolume = 0.5f;
    private float _sfxVolume = 0.8f;

    private AudioLayer? _baseDrone;
    private AudioLayer? _timeOfDayLayer;
    private AudioLayer? _seasonLayer;
    private AudioLayer? _eventLayer;
    private AudioLayer? _signatureMotif;

    private string _currentTimeOfDay = "morning";
    private string _currentSeason = "spring";
    private int _sanctuaryLevel;

    public float MasterVolume
    {
        get => _masterVolume;
        set => _masterVolume = Math.Clamp(value, 0f, 1f);
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set => _musicVolume = Math.Clamp(value, 0f, 1f);
    }

    public float SfxVolume
    {
        get => _sfxVolume;
        set => _sfxVolume = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Update the soundscape based on current game state.
    /// Crossfades layers as time-of-day and season change.
    /// </summary>
    public void Update(string timeOfDay, string season, int sanctuaryLevel, float deltaTime)
    {
        // Time-of-day crossfade
        if (timeOfDay != _currentTimeOfDay)
        {
            _currentTimeOfDay = timeOfDay;
            CrossfadeTimeOfDayLayer(timeOfDay);
        }

        // Season crossfade
        if (season != _currentSeason)
        {
            _currentSeason = season;
            CrossfadeSeasonLayer(season);
        }

        // Signature motif evolves with sanctuary level
        if (sanctuaryLevel != _sanctuaryLevel)
        {
            _sanctuaryLevel = sanctuaryLevel;
            UpdateSignatureMotif(sanctuaryLevel);
        }

        // Update all layer volumes
        UpdateLayerVolumes(deltaTime);
    }

    /// <summary>Play a one-shot SFX (spell cast, building placed, etc).</summary>
    public void PlaySfx(SfxType type)
    {
        // In production: load and play the appropriate audio clip
        // For now, this is the structural API
    }

    private void CrossfadeTimeOfDayLayer(string timeOfDay)
    {
        // Crossfade from current to new time-of-day audio layer
        // Layers: dawn_chirps, morning_birds, afternoon_breeze, dusk_wind, night_crickets, deepnight_silence
    }

    private void CrossfadeSeasonLayer(string season)
    {
        // Crossfade seasonal ambient layer
        // Layers: spring_rain, summer_cicadas, autumn_leaves, winter_wind
    }

    private void UpdateSignatureMotif(int level)
    {
        // The signature musical phrase evolves as the sanctuary grows:
        // Level 1-2: Single chime, sparse
        // Level 3-4: Two-part harmony
        // Level 5-6: Full melody with bass
        // Level 7-8: Orchestral swell
        // Level 9-10: Complete arrangement with choir-like pads
    }

    private void UpdateLayerVolumes(float deltaTime)
    {
        // Smooth volume transitions between layers
    }
}

/// <summary>Represents a single audio layer with crossfade capability.</summary>
public sealed class AudioLayer
{
    public string Name { get; init; } = "";
    public float TargetVolume { get; set; }
    public float CurrentVolume { get; set; }
    public bool IsPlaying { get; set; }
    public float CrossfadeSpeed { get; set; } = 1f; // seconds to crossfade
}

/// <summary>Sound effect types.</summary>
public enum SfxType
{
    SpellCast,
    SpellFail,
    ComboComplete,
    BuildingPlace,
    BuildingUpgrade,
    ItemCraft,
    FamiliarHatch,
    FamiliarHappy,
    FamiliarSad,
    ExpeditionSend,
    ExpeditionReturn,
    GrimoireUnlock,
    MenuOpen,
    MenuClose,
    ButtonClick,
    Notification
}

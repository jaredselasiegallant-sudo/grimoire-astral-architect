using SkiaSharp;

namespace Grimoire.Engine.Juice;

/// <summary>
/// "Juice" system — screen shake, particle bursts, colour flashes,
/// and other feel-good feedback that makes every action satisfying.
/// This is where award submissions are won or lost.
/// </summary>
public sealed class JuiceEngine
{
    // Screen shake state
    private float _shakeIntensity;
    private float _shakeDuration;
    private float _shakeTimer;
    private float _shakeOffsetX;
    private float _shakeOffsetY;

    // Colour flash state
    private SKColor _flashColor = SKColors.Transparent;
    private float _flashDuration;
    private float _flashTimer;

    // Slow-motion state
    private float _slowMotionScale = 1f;
    private float _slowMotionDuration;
    private float _slowMotionTimer;

    private readonly Random _rng = new();

    /// <summary>Current screen shake offset to apply to rendering.</summary>
    public (float X, float Y) ShakeOffset => (_shakeOffsetX, _shakeOffsetY);

    /// <summary>Current time scale (1 = normal, 0.5 = slow-mo).</summary>
    public float TimeScale => _slowMotionScale;

    /// <summary>Whether a colour flash is active.</summary>
    public bool IsFlashing => _flashTimer > 0;

    /// <summary>Current flash colour.</summary>
    public SKColor FlashColor => _flashColor;

    /// <summary>Flash alpha (0-1).</summary>
    public float FlashAlpha => _flashDuration > 0 ? _flashTimer / _flashDuration : 0;

    // ─── Triggers ────────────────────────────────────────────────

    /// <summary>Trigger screen shake. Called on spell cast, building placement, etc.</summary>
    public void Shake(float intensity = 5f, float duration = 0.3f)
    {
        _shakeIntensity = Math.Max(_shakeIntensity, intensity);
        _shakeDuration = duration;
        _shakeTimer = duration;
    }

    /// <summary>Trigger a full-screen colour flash.</summary>
    public void Flash(SKColor color, float duration = 0.2f)
    {
        _flashColor = color;
        _flashDuration = duration;
        _flashTimer = duration;
    }

    /// <summary>Trigger slow-motion effect.</summary>
    public void SlowMotion(float scale = 0.3f, float duration = 0.5f)
    {
        _slowMotionScale = scale;
        _slowMotionDuration = duration;
        _slowMotionTimer = duration;
    }

    /// <summary>Trigger a particle burst at a position (delegates to ParticleSystem).</summary>
    public ParticleBurstRequest? RequestBurst(float x, float y, SKColor color, string type = "default")
    {
        return new ParticleBurstRequest
        {
            X = x,
            Y = y,
            Color = color,
            Count = type switch
            {
                "spell_cast" => 30,
                "building_place" => 20,
                "hatch" => 50,
                "combo" => 40,
                "discovery" => 60,
                _ => 15
            },
            Speed = type switch
            {
                "spell_cast" => 150f,
                "combo" => 200f,
                "discovery" => 100f,
                _ => 80f
            },
            Lifetime = type switch
            {
                "hatch" => 2.0f,
                "discovery" => 2.5f,
                _ => 1.0f
            }
        };
    }

    // ─── Update ──────────────────────────────────────────────────

    /// <summary>Update all juice effects. Call once per frame with deltaTime.</summary>
    public void Update(float deltaTime)
    {
        // Screen shake
        if (_shakeTimer > 0)
        {
            _shakeTimer -= deltaTime;
            var progress = _shakeTimer / _shakeDuration;
            var currentIntensity = _shakeIntensity * progress;

            _shakeOffsetX = (float)(_rng.NextDouble() * 2 - 1) * currentIntensity;
            _shakeOffsetY = (float)(_rng.NextDouble() * 2 - 1) * currentIntensity;
        }
        else
        {
            _shakeOffsetX = 0;
            _shakeOffsetY = 0;
            _shakeIntensity = 0;
        }

        // Colour flash
        if (_flashTimer > 0)
            _flashTimer -= deltaTime;

        // Slow motion
        if (_slowMotionTimer > 0)
        {
            _slowMotionTimer -= deltaTime;
            if (_slowMotionTimer <= 0)
                _slowMotionScale = 1f;
        }
    }

    // ─── Convenience Methods ─────────────────────────────────────

    /// <summary>Call when a spell is successfully cast.</summary>
    public void OnSpellCast(string gestureName)
    {
        var color = gestureName switch
        {
            "Circle" => new SKColor(100, 200, 255),
            "Triangle" => new SKColor(255, 100, 200),
            "Line" => new SKColor(200, 255, 100),
            "Zigzag" => new SKColor(255, 200, 100),
            "Spiral" => new SKColor(200, 100, 255),
            _ => new SKColor(255, 255, 255)
        };
        Shake(4f, 0.2f);
        Flash(color, 0.15f);
    }

    /// <summary>Call when a combo is completed.</summary>
    public void OnComboComplete()
    {
        Shake(8f, 0.4f);
        Flash(new SKColor(255, 230, 100), 0.3f);
        SlowMotion(0.3f, 0.5f);
    }

    /// <summary>Call when a familiar hatches.</summary>
    public void OnHatch()
    {
        Shake(6f, 0.3f);
        Flash(new SKColor(255, 200, 100), 0.4f);
    }

    /// <summary>Call when a discovery is made in the Grimoire.</summary>
    public void OnDiscovery()
    {
        Shake(3f, 0.2f);
        Flash(new SKColor(200, 100, 255), 0.25f);
    }

    /// <summary>Call when corruption changes visually.</summary>
    public void OnCorruptionPulse()
    {
        Shake(2f, 0.5f);
        Flash(new SKColor(60, 20, 80), 0.6f);
    }
}

public sealed class ParticleBurstRequest
{
    public float X { get; init; }
    public float Y { get; init; }
    public SKColor Color { get; init; }
    public int Count { get; init; }
    public float Speed { get; init; }
    public float Lifetime { get; init; }
}

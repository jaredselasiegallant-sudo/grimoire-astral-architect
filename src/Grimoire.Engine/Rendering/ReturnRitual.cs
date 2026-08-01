using SkiaSharp;

namespace Grimoire.Engine.Rendering;

/// <summary>
/// The Return Ritual — a beautiful, unskippable moment every time
/// the player reopens the app. The sanctuary "wakes up":
/// the skybox transitions from dark to current time-of-day,
/// the shrine pulses back to life, and familiars stir.
/// 
/// This resets the emotional tone each session and creates
/// a moment of calm before gameplay begins.
/// </summary>
public sealed class ReturnRitual
{
    private float _progress; // 0 to 1
    private bool _isActive;
    private float _totalDuration = 3.5f; // seconds

    /// <summary>How far through the ritual we are (0-1).</summary>
    public float Progress => _progress;

    /// <summary>Whether the ritual is currently playing.</summary>
    public bool IsActive => _isActive;

    /// <summary>Phase of the ritual for rendering decisions.</summary>
    public RitualPhase CurrentPhase => _progress switch
    {
        < 0.15f => RitualPhase.Darkness,
        < 0.35f => RitualPhase.FirstLight,
        < 0.55f => RitualPhase.SkyBrightening,
        < 0.75f => RitualPhase.ShrinePulse,
        < 0.90f => RitualPhase.FamiliarsStir,
        _ => RitualPhase.Complete
    };

    /// <summary>Fired when the ritual completes.</summary>
    public event Action? OnRitualComplete;

    /// <summary>Start the return ritual.</summary>
    public void Start()
    {
        _progress = 0;
        _isActive = true;
    }

    /// <summary>Update the ritual. Call each frame with deltaTime.</summary>
    public void Update(float deltaTime)
    {
        if (!_isActive) return;

        _progress += deltaTime / _totalDuration;

        if (_progress >= 1f)
        {
            _progress = 1f;
            _isActive = false;
            OnRitualComplete?.Invoke();
        }
    }

    /// <summary>
    /// Render the ritual overlay effect.
    /// Draws a dark-to-light transition with shrine glow and particle effects.
    /// </summary>
    public void Render(SKCanvas canvas, int width, int height, double elapsedSeconds)
    {
        if (!_isActive) return;

        var phase = CurrentPhase;

        // Phase 1: Darkness — black overlay that fades
        if (phase <= RitualPhase.FirstLight)
        {
            var alpha = (byte)(255 * (1f - _progress / 0.35f));
            using var darkPaint = new SKPaint { Color = new SKColor(5, 5, 15, alpha) };
            canvas.DrawRect(0, 0, width, height, darkPaint);
        }

        // Phase 2: First Light — a single point of light appears at center
        if (phase >= RitualPhase.FirstLight && phase <= RitualPhase.SkyBrightening)
        {
            var lightProgress = (_progress - 0.15f) / 0.40f;
            var radius = lightProgress * Math.Min(width, height) * 0.6f;
            var alpha = (byte)(200 * lightProgress);

            using var lightPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
                Color = new SKColor(100, 200, 255, alpha)
            };

            // Radial glow from center
            using var shader = SKShader.CreateRadialGradient(
                new SKPoint(width / 2f, height / 2f),
                radius,
                [new SKColor(100, 200, 255, alpha), new SKColor(100, 200, 255, 0)],
                [0f, 1f],
                SKShaderTileMode.Clamp);

            lightPaint.Shader = shader;
            canvas.DrawRect(0, 0, width, height, lightPaint);
        }

        // Phase 3: Shrine Pulse — concentric rings expand from center
        if (phase >= RitualPhase.ShrinePulse && phase <= RitualPhase.FamiliarsStir)
        {
            var pulseProgress = (_progress - 0.55f) / 0.35f;
            using var ringPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2f,
                IsAntialias = true
            };

            for (int i = 0; i < 3; i++)
            {
                var ringProgress = Math.Max(0, pulseProgress - i * 0.15f);
                if (ringProgress <= 0) continue;

                var ringRadius = ringProgress * 200f;
                var ringAlpha = (byte)(180 * (1f - ringProgress));
                ringPaint.Color = new SKColor(100, 200, 255, ringAlpha);
                canvas.DrawCircle(width / 2f, height / 2f, ringRadius, ringPaint);
            }
        }

        // Phase 4: Familiars Stir — floating particles drift upward
        if (phase >= RitualPhase.FamiliarsStir)
        {
            using var particlePaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            var rng = new Random(42);
            for (int i = 0; i < 20; i++)
            {
                var x = width * 0.3f + (float)rng.NextDouble() * width * 0.4f;
                var baseY = height * 0.6f;
                var drift = (float)(elapsedSeconds * 30 + rng.NextDouble() * 50);
                var y = baseY - drift * ((_progress - 0.75f) / 0.25f);
                var alpha = (byte)(150 * (1f - Math.Abs(y - height * 0.4f) / (height * 0.4f)));
                alpha = Math.Clamp(alpha, (byte)0, (byte)200);

                particlePaint.Color = new SKColor(100, 200, 255, alpha);
                canvas.DrawCircle(x, y, 1.5f + (float)rng.NextDouble() * 2f, particlePaint);
            }
        }
    }
}

public enum RitualPhase
{
    Darkness,
    FirstLight,
    SkyBrightening,
    ShrinePulse,
    FamiliarsStir,
    Complete
}

using SkiaSharp;

namespace Grimoire.Engine.Ecology;

/// <summary>
/// Living Particle Ecology — ambient particles (dust motes, starlight, mana wisps)
/// react to cursor proximity and familiar movement, drifting away or gathering.
/// The world feels like it notices you even when idle.
/// </summary>
public sealed class ParticleEcology
{
    private readonly List<EcologyParticle> _particles = [];
    private readonly Random _rng = new(42);

    private float _cursorX = -1000;
    private float _cursorY = -1000;
    private bool _cursorActive;

    private const int MaxParticles = 300;
    private const float CursorRepelRadius = 80f;
    private const float CursorAttractRadius = 150f;

    /// <summary>Set the current cursor position (call on pointer move).</summary>
    public void SetCursor(float x, float y, bool active)
    {
        _cursorX = x;
        _cursorY = y;
        _cursorActive = active;
    }

    /// <summary>Spawn ambient particles across the canvas.</summary>
    public void Initialise(int canvasWidth, int canvasHeight)
    {
        _particles.Clear();

        // Dust motes
        for (int i = 0; i < 80; i++)
            _particles.Add(CreateParticle(canvasWidth, canvasHeight, ParticleType.Dust));

        // Starlight sparkles
        for (int i = 0; i < 40; i++)
            _particles.Add(CreateParticle(canvasWidth, canvasHeight, ParticleType.Starlight));

        // Mana wisps
        for (int i = 0; i < 20; i++)
            _particles.Add(CreateParticle(canvasWidth, canvasHeight, ParticleType.ManaWisp));
    }

    /// <summary>Update all particles. Call each frame.</summary>
    public void Update(float deltaTime, int canvasWidth, int canvasHeight)
    {
        foreach (var p in _particles)
        {
            // Cursor interaction
            if (_cursorActive)
            {
                var dx = p.X - _cursorX;
                var dy = p.Y - _cursorY;
                var dist = MathF.Sqrt(dx * dx + dy * dy);

                if (p.Type == ParticleType.Dust || p.Type == ParticleType.Starlight)
                {
                    // Dust and starlight flee from cursor
                    if (dist < CursorRepelRadius && dist > 0.01f)
                    {
                        var force = (1f - dist / CursorRepelRadius) * 60f;
                        p.VelX += (dx / dist) * force * deltaTime;
                        p.VelY += (dy / dist) * force * deltaTime;
                    }
                }
                else if (p.Type == ParticleType.ManaWisp)
                {
                    // Mana wisps are curious — they approach then orbit
                    if (dist < CursorAttractRadius && dist > 20f)
                    {
                        var force = (1f - dist / CursorAttractRadius) * 25f;
                        p.VelX -= (dx / dist) * force * deltaTime;
                        p.VelY -= (dy / dist) * force * deltaTime;
                    }
                    else if (dist <= 20f)
                    {
                        // Orbit around cursor
                        var angle = MathF.Atan2(dy, dx) + deltaTime * 2f;
                        p.VelX = MathF.Cos(angle) * 30f - p.VelX * 0.9f;
                        p.VelY = MathF.Sin(angle) * 30f - p.VelY * 0.9f;
                    }
                }
            }

            // Natural drift
            p.VelX += (float)(_rng.NextDouble() - 0.5) * 5f * deltaTime;
            p.VelY += (float)(_rng.NextDouble() - 0.5) * 5f * deltaTime;

            // Gentle gravity for dust
            if (p.Type == ParticleType.Dust)
                p.VelY += 2f * deltaTime;

            // Damping
            p.VelX *= 0.98f;
            p.VelY *= 0.98f;

            // Move
            p.X += p.VelX * deltaTime;
            p.Y += p.VelY * deltaTime;

            // Wrap around edges
            if (p.X < -20) p.X = canvasWidth + 20;
            if (p.X > canvasWidth + 20) p.X = -20;
            if (p.Y < -20) p.Y = canvasHeight + 20;
            if (p.Y > canvasHeight + 20) p.Y = -20;

            // Twinkle
            p.Alpha = (byte)(p.BaseAlpha * (0.5f + 0.5f * MathF.Sin(p.TwinklePhase + p.TwinkleSpeed)));
            p.TwinklePhase += deltaTime * p.TwinkleSpeed;
        }
    }

    /// <summary>Render all ecology particles.</summary>
    public void Render(SKCanvas canvas)
    {
        using var paint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };

        foreach (var p in _particles)
        {
            paint.Color = new SKColor(p.Color.Red, p.Color.Green, p.Color.Blue, p.Alpha);
            canvas.DrawCircle(p.X, p.Y, p.Size, paint);
        }
    }

    private EcologyParticle CreateParticle(int w, int h, ParticleType type)
    {
        var color = type switch
        {
            ParticleType.Dust => new SKColor(180, 170, 140),
            ParticleType.Starlight => new SKColor(255, 255, 240),
            ParticleType.ManaWisp => new SKColor(100, 200, 255),
            _ => new SKColor(200, 200, 200)
        };

        return new EcologyParticle
        {
            X = (float)(_rng.NextDouble() * w),
            Y = (float)(_rng.NextDouble() * h),
            VelX = 0,
            VelY = 0,
            Color = color,
            Type = type,
            Size = type switch
            {
                ParticleType.Dust => 1f + (float)_rng.NextDouble() * 1.5f,
                ParticleType.Starlight => 0.5f + (float)_rng.NextDouble() * 1f,
                ParticleType.ManaWisp => 2f + (float)_rng.NextDouble() * 2f,
                _ => 1f
            },
            BaseAlpha = type switch
            {
                ParticleType.Dust => (byte)(40 + _rng.Next(0, 40)),
                ParticleType.Starlight => (byte)(60 + _rng.Next(0, 100)),
                ParticleType.ManaWisp => (byte)(80 + _rng.Next(0, 80)),
                _ => (byte)100
            },
            Alpha = (byte)100,
            TwinklePhase = (float)(_rng.NextDouble() * Math.PI * 2),
            TwinkleSpeed = 1f + (float)_rng.NextDouble() * 3f
        };
    }
}

public sealed class EcologyParticle
{
    public float X, Y;
    public float VelX, VelY;
    public SKColor Color;
    public ParticleType Type;
    public float Size;
    public byte BaseAlpha;
    public byte Alpha;
    public float TwinklePhase;
    public float TwinkleSpeed;
}

public enum ParticleType
{
    Dust,
    Starlight,
    ManaWisp
}

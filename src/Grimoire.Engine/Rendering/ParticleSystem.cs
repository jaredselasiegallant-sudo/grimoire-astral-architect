using SkiaSharp;

namespace Grimoire.Engine.Rendering;

/// <summary>
/// Lightweight GPU-friendly particle system for spell effects, ambient sparkles,
/// and building glow halos. All particles are CPU-side for offline simplicity.
///
/// Each particle is a simple struct with position, velocity, colour, and lifetime.
/// The system supports multiple emitters running concurrently.
/// </summary>
public sealed class ParticleSystem
{
    private readonly List<Particle> _particles = [];
    private readonly List<Emitter> _emitters = [];

    // Performance cap
    private const int MaxParticles = 2000;

    private readonly SKPaint _particlePaint;

    public int ActiveParticleCount => _particles.Count;

    public ParticleSystem()
    {
        _particlePaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
    }

    // ─── Public API ──────────────────────────────────────────────

    /// <summary>Register a persistent emitter (e.g. building glow, ambient).</summary>
    public void AddEmitter(Emitter emitter)
    {
        _emitters.Add(emitter);
    }

    /// <summary>Remove all emitters and particles.</summary>
    public void Clear()
    {
        _emitters.Clear();
        _particles.Clear();
    }

    /// <summary>One-shot burst of particles (e.g. on spell cast).</summary>
    public void Burst(float x, float y, SKColor color, int count, float speed = 100f, float lifetime = 1.0f)
    {
        var rng = Random.Shared;
        for (int i = 0; i < count && _particles.Count < MaxParticles; i++)
        {
            var angle = (float)(rng.NextDouble() * Math.PI * 2);
            var vel = speed * (0.5f + (float)rng.NextDouble() * 0.5f);

            _particles.Add(new Particle
            {
                X = x,
                Y = y,
                VelX = MathF.Cos(angle) * vel,
                VelY = MathF.Sin(angle) * vel,
                Color = color,
                Life = lifetime,
                MaxLife = lifetime,
                Size = 2f + (float)rng.NextDouble() * 4f
            });
        }
    }

    /// <summary>Update all particles and emitters. Call once per frame.</summary>
    public void Update(float deltaTime)
    {
        // Emit new particles from active emitters
        foreach (var emitter in _emitters)
        {
            emitter.Accumulator += deltaTime;
            while (emitter.Accumulator >= emitter.Interval && _particles.Count < MaxParticles)
            {
                emitter.Accumulator -= emitter.Interval;
                EmitFrom(emitter);
            }
        }

        // Update existing particles
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];

            p.Life -= deltaTime;
            if (p.Life <= 0)
            {
                _particles.RemoveAt(i);
                continue;
            }

            p.X += p.VelX * deltaTime;
            p.Y += p.VelY * deltaTime;

            // Gentle gravity pull
            p.VelY += 20f * deltaTime;

            // Damping
            p.VelX *= 0.99f;
            p.VelY *= 0.99f;
        }
    }

    /// <summary>Render all active particles to the canvas.</summary>
    public void Draw(SKCanvas canvas)
    {
        foreach (var p in _particles)
        {
            var alpha = (byte)(255 * (p.Life / p.MaxLife));
            _particlePaint.Color = new SKColor(p.Color.Red, p.Color.Green, p.Color.Blue, alpha);

            var radius = p.Size * (p.Life / p.MaxLife);
            canvas.DrawCircle(p.X, p.Y, radius, _particlePaint);
        }
    }

    private void EmitFrom(Emitter emitter)
    {
        var rng = Random.Shared;
        var angle = (float)(rng.NextDouble() * Math.PI * 2);
        var speed = emitter.Speed * (0.5f + (float)rng.NextDouble() * 0.5f);

        _particles.Add(new Particle
        {
            X = emitter.X + (float)(rng.NextDouble() * emitter.Spread - emitter.Spread / 2),
            Y = emitter.Y + (float)(rng.NextDouble() * emitter.Spread - emitter.Spread / 2),
            VelX = MathF.Cos(angle) * speed,
            VelY = MathF.Sin(angle) * speed,
            Color = emitter.Color,
            Life = emitter.Lifetime,
            MaxLife = emitter.Lifetime,
            Size = emitter.Size * (0.5f + (float)rng.NextDouble() * 0.5f)
        });
    }

    // ─── Types ───────────────────────────────────────────────────

    public struct Particle
    {
        public float X, Y;
        public float VelX, VelY;
        public SKColor Color;
        public float Life, MaxLife;
        public float Size;
    }

    public class Emitter
    {
        public float X { get; set; }
        public float Y { get; set; }
        public SKColor Color { get; set; } = new(100, 200, 255);
        public float Speed { get; set; } = 40f;
        public float Lifetime { get; set; } = 1.5f;
        public float Size { get; set; } = 3f;
        public float Spread { get; set; } = 10f;
        public float Interval { get; set; } = 0.1f; // seconds between emissions
        public float Accumulator { get; set; }
    }
}

namespace Grimoire.Engine.GameLoop;

/// <summary>
/// High-resolution game loop that ticks at a target frame rate.
/// Drives the update → render cycle on a background thread,
/// posting frame callbacks to the UI thread for canvas painting.
/// </summary>
public sealed class GameLoopService : IDisposable
{
    /// <summary>Target frames per second.</summary>
    public int TargetFps { get; set; } = 60;

    /// <summary>Elapsed time in seconds since the loop started.</summary>
    public double ElapsedTime { get; private set; }

    /// <summary>Delta time for the current frame in seconds.</summary>
    public float DeltaTime { get; private set; }

    /// <summary>Total frames rendered since start.</summary>
    public long FrameCount { get; private set; }

    /// <summary>Whether the loop is currently running.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>Fired every frame. Subscribers receive (deltaTime, elapsedTime).</summary>
    public event Action<float, double>? FrameTick;

    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private readonly System.Diagnostics.Stopwatch _stopwatch = new();
    private double _lastTickTime;

    // ─── Lifecycle ───────────────────────────────────────────────

    /// <summary>Starts the game loop on a background thread.</summary>
    public void Start()
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        _stopwatch.Restart();
        _lastTickTime = 0;
        ElapsedTime = 0;
        FrameCount = 0;
        IsRunning = true;

        _loopTask = Task.Run(() => RunLoop(_cts.Token));
    }

    /// <summary>Stops the game loop and waits for the current frame to finish.</summary>
    public async Task StopAsync()
    {
        if (!IsRunning) return;

        _cts?.Cancel();
        if (_loopTask != null)
            await _loopTask;

        _stopwatch.Stop();
        IsRunning = false;
    }

    // ─── Loop ────────────────────────────────────────────────────

    private void RunLoop(CancellationToken ct)
    {
        var frameInterval = 1.0 / TargetFps;

        while (!ct.IsCancellationRequested)
        {
            var now = _stopwatch.Elapsed.TotalSeconds;
            var frameStart = now;

            DeltaTime = (float)(now - _lastTickTime);
            _lastTickTime = now;
            ElapsedTime = now;
            FrameCount++;

            // Notify subscribers
            FrameTick?.Invoke(DeltaTime, ElapsedTime);

            // Sleep for remaining frame time (busy-wait for precision on last ~1ms)
            var elapsed = _stopwatch.Elapsed.TotalSeconds - frameStart;
            var remaining = frameInterval - elapsed;

            if (remaining > 0.001)
            {
                Thread.Sleep(TimeSpan.FromSeconds(remaining * 0.8));
            }

            // Busy-wait for precise timing on the final stretch
            while (_stopwatch.Elapsed.TotalSeconds - frameStart < frameInterval)
            {
                Thread.SpinWait(100);
            }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}

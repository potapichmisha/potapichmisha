using System.Diagnostics;

namespace DotaAssistant.Capture;

/// <summary>
/// Fallback when DXGI init fails (e.g. remote session / no adapter).
/// Produces synthetic frame timestamps so the UI shell still demonstrates the pipeline.
/// </summary>
public sealed class StubScreenCapture : IScreenCapture
{
    private readonly object _sync = new();
    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;
    private long _frameCount;
    private DateTimeOffset? _lastFrameAt;
    private double _fps;
    private string _status = "Остановлен";
    private readonly string _reason;

    public StubScreenCapture(string reason = "DXGI недоступен")
    {
        _reason = reason;
    }

    public CaptureStats GetStats()
    {
        lock (_sync)
        {
            return new CaptureStats
            {
                Fps = _fps,
                LastFrameAt = _lastFrameAt,
                FrameCount = _frameCount,
                Status = _status,
                Backend = $"Заглушка ({_reason})"
            };
        }
    }

    public bool TryCloneLatestPixels(out CapturePixelFrame? frame)
    {
        frame = null;
        return false;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_loopTask is { IsCompleted: false })
            {
                return Task.CompletedTask;
            }

            _status = "Имитация захвата…";
            _loopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _loopCts.Token;
            _loopTask = Task.Run(() => Loop(token), token);
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        CancellationTokenSource? cts;
        Task? loop;
        lock (_sync)
        {
            cts = _loopCts;
            loop = _loopTask;
            _loopCts = null;
            _loopTask = null;
        }

        cts?.Cancel();
        if (loop is not null)
        {
            try
            {
                await loop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        lock (_sync)
        {
            _status = "Остановлен";
            _fps = 0;
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

    private void Loop(CancellationToken token)
    {
        var sw = Stopwatch.StartNew();
        var windowFrames = 0;
        var window = Stopwatch.StartNew();

        while (!token.IsCancellationRequested)
        {
            // ~15 Hz synthetic cadence — enough for UI stats, not a real capture budget
            Thread.Sleep(66);
            lock (_sync)
            {
                _frameCount++;
                _lastFrameAt = DateTimeOffset.Now;
                windowFrames++;
                if (window.Elapsed.TotalSeconds >= 1.0)
                {
                    _fps = windowFrames / window.Elapsed.TotalSeconds;
                    windowFrames = 0;
                    window.Restart();
                }

                _status = $"Имитация захвата… ({sw.Elapsed:mm\\:ss})";
            }
        }
    }
}

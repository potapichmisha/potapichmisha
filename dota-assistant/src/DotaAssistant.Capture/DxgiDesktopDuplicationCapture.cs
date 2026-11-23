using System.Diagnostics;
using SharpGen.Runtime;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace DotaAssistant.Capture;

/// <summary>
/// DXGI Desktop Duplication capture loop. V0 acquires frames for FPS/timestamp only;
/// no vision analysis and no game process interaction.
/// </summary>
public sealed class DxgiDesktopDuplicationCapture : IScreenCapture
{
    private readonly object _sync = new();
    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;

    private ID3D11Device? _device;
    private ID3D11DeviceContext? _context;
    private IDXGIOutputDuplication? _duplication;
    private int _width;
    private int _height;

    private long _frameCount;
    private DateTimeOffset? _lastFrameAt;
    private double _fps;
    private string _status = "Остановлен";
    private readonly Stopwatch _fpsWindow = new();
    private int _fpsWindowFrames;

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
                Backend = "DXGI Desktop Duplication"
            };
        }
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_loopTask is { IsCompleted: false })
            {
                return Task.CompletedTask;
            }

            try
            {
                InitializeDuplication();
                _status = "Захват…";
            }
            catch (Exception ex)
            {
                _status = $"Ошибка DXGI: {ex.Message}";
                throw;
            }

            _loopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _loopCts.Token;
            _loopTask = Task.Run(() => CaptureLoop(token), token);
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
            _status = "Остановка…";
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
                // expected
            }
        }

        lock (_sync)
        {
            ReleaseDuplication();
            _status = "Остановлен";
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }

    private void InitializeDuplication()
    {
        ReleaseDuplication();

        using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
        factory.EnumAdapters1(0, out IDXGIAdapter1 adapter).CheckError();
        using (adapter)
        {
            adapter.EnumOutputs(0, out IDXGIOutput output).CheckError();
            using (output)
            {
                using var output1 = output.QueryInterface<IDXGIOutput1>();

                D3D11.D3D11CreateDevice(
                    adapter,
                    DriverType.Unknown,
                    DeviceCreationFlags.BgraSupport,
                    new[] { FeatureLevel.Level_11_0, FeatureLevel.Level_10_0 },
                    out _device,
                    out _context).CheckError();

                if (_device is null || _context is null)
                {
                    throw new InvalidOperationException("Не удалось создать устройство D3D11.");
                }

                _duplication = output1.DuplicateOutput(_device);
                var desc = output.Description;
                _width = desc.DesktopCoordinates.Right - desc.DesktopCoordinates.Left;
                _height = desc.DesktopCoordinates.Bottom - desc.DesktopCoordinates.Top;
            }
        }

        _frameCount = 0;
        _lastFrameAt = null;
        _fps = 0;
        _fpsWindowFrames = 0;
        _fpsWindow.Restart();

        // Silence unused field warnings until ROI work uses dimensions.
        _ = _width;
        _ = _height;
    }

    private void CaptureLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (_duplication is null)
                {
                    Thread.Sleep(50);
                    continue;
                }

                var result = _duplication.AcquireNextFrame(100, out _, out var desktopResource);
                if (result.Failure)
                {
                    if (result == Vortice.DXGI.ResultCode.WaitTimeout)
                    {
                        continue;
                    }

                    if (result == Vortice.DXGI.ResultCode.AccessLost)
                    {
                        lock (_sync)
                        {
                            _status = "Переинициализация DXGI…";
                            try
                            {
                                InitializeDuplication();
                                _status = "Захват…";
                            }
                            catch (Exception ex)
                            {
                                _status = $"Ошибка DXGI: {ex.Message}";
                                Thread.Sleep(500);
                            }
                        }

                        continue;
                    }

                    lock (_sync)
                    {
                        _status = $"DXGI: {result}";
                    }

                    Thread.Sleep(50);
                    continue;
                }

                try
                {
                    desktopResource?.Dispose();
                    _duplication.ReleaseFrame();

                    lock (_sync)
                    {
                        _frameCount++;
                        _lastFrameAt = DateTimeOffset.Now;
                        _fpsWindowFrames++;
                        var elapsed = _fpsWindow.Elapsed.TotalSeconds;
                        if (elapsed >= 1.0)
                        {
                            _fps = _fpsWindowFrames / elapsed;
                            _fpsWindowFrames = 0;
                            _fpsWindow.Restart();
                        }

                        _status = "Захват…";
                    }
                }
                catch
                {
                    try { _duplication.ReleaseFrame(); } catch { /* ignore */ }
                    throw;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lock (_sync)
                {
                    _status = $"Ошибка цикла: {ex.Message}";
                }

                Thread.Sleep(250);
            }
        }
    }

    private void ReleaseDuplication()
    {
        _duplication?.Dispose();
        _duplication = null;
        _context?.Dispose();
        _context = null;
        _device?.Dispose();
        _device = null;
    }
}

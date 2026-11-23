using System.Diagnostics;
using SharpGen.Runtime;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using MapFlags = Vortice.Direct3D11.MapFlags;

namespace DotaAssistant.Capture;

/// <summary>
/// DXGI Desktop Duplication capture loop. Tracks FPS and periodically copies a BGRA
/// frame to CPU memory for local vision (no game process interaction).
/// </summary>
public sealed class DxgiDesktopDuplicationCapture : IScreenCapture
{
    private const int PixelCopyIntervalMs = 400;

    private readonly object _sync = new();
    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;

    private ID3D11Device? _device;
    private ID3D11DeviceContext? _context;
    private IDXGIOutputDuplication? _duplication;
    private ID3D11Texture2D? _staging;
    private int _width;
    private int _height;

    private long _frameCount;
    private DateTimeOffset? _lastFrameAt;
    private double _fps;
    private string _status = "Остановлен";
    private readonly Stopwatch _fpsWindow = new();
    private int _fpsWindowFrames;
    private readonly Stopwatch _pixelCopyWatch = new();

    private byte[]? _latestBgra;
    private int _latestWidth;
    private int _latestHeight;
    private int _latestPitch;
    private DateTimeOffset _latestPixelAt;
    private long _latestPixelFrameIndex;

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

    public bool TryCloneLatestPixels(out CapturePixelFrame? frame)
    {
        lock (_sync)
        {
            if (_latestBgra is null || _latestWidth <= 0 || _latestHeight <= 0)
            {
                frame = null;
                return false;
            }

            var clone = new byte[_latestBgra.Length];
            Buffer.BlockCopy(_latestBgra, 0, clone, 0, _latestBgra.Length);
            frame = new CapturePixelFrame
            {
                Width = _latestWidth,
                Height = _latestHeight,
                RowPitch = _latestPitch,
                BgraPixels = clone,
                CapturedAt = _latestPixelAt,
                FrameIndex = _latestPixelFrameIndex
            };
            return true;
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
        _pixelCopyWatch.Restart();
        _latestBgra = null;
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
                    if (desktopResource is not null
                        && _pixelCopyWatch.ElapsedMilliseconds >= PixelCopyIntervalMs
                        && _device is not null
                        && _context is not null)
                    {
                        try
                        {
                            using var texture = desktopResource.QueryInterface<ID3D11Texture2D>();
                            CopyTextureToCpu(texture);
                            _pixelCopyWatch.Restart();
                        }
                        catch (Exception ex)
                        {
                            lock (_sync)
                            {
                                _status = $"Копия кадра: {ex.Message}";
                            }
                        }
                    }

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

                        if (_status.StartsWith("Копия кадра:", StringComparison.Ordinal))
                        {
                            // keep copy error visible briefly
                        }
                        else
                        {
                            _status = _latestBgra is null ? "Захват… (ждём пиксели)" : "Захват…";
                        }
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

    private void CopyTextureToCpu(ID3D11Texture2D source)
    {
        if (_device is null || _context is null)
        {
            return;
        }

        var desc = source.Description;
        EnsureStaging(desc.Width, desc.Height, desc.Format);
        if (_staging is null)
        {
            return;
        }

        _context.CopyResource(_staging, source);
        var mapped = _context.Map(_staging, 0, MapMode.Read, MapFlags.None);
        try
        {
            var pitch = (int)mapped.RowPitch;
            var height = (int)desc.Height;
            var width = (int)desc.Width;
            var needed = pitch * height;
            var buffer = new byte[needed];
            System.Runtime.InteropServices.Marshal.Copy(mapped.DataPointer, buffer, 0, needed);

            lock (_sync)
            {
                _latestBgra = buffer;
                _latestWidth = width;
                _latestHeight = height;
                _latestPitch = pitch;
                _latestPixelAt = DateTimeOffset.Now;
                _latestPixelFrameIndex = _frameCount + 1;
            }
        }
        finally
        {
            _context.Unmap(_staging, 0);
        }
    }

    private void EnsureStaging(uint width, uint height, Format format)
    {
        if (_device is null)
        {
            return;
        }

        if (_staging is not null)
        {
            var existing = _staging.Description;
            if (existing.Width == width && existing.Height == height && existing.Format == format)
            {
                return;
            }

            _staging.Dispose();
            _staging = null;
        }

        var stagingDesc = new Texture2DDescription
        {
            Width = width,
            Height = height,
            MipLevels = 1,
            ArraySize = 1,
            Format = format,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging,
            BindFlags = BindFlags.None,
            CPUAccessFlags = CpuAccessFlags.Read,
            MiscFlags = ResourceOptionFlags.None
        };

        _staging = _device.CreateTexture2D(stagingDesc);
        _width = (int)width;
        _height = (int)height;
    }

    private void ReleaseDuplication()
    {
        _staging?.Dispose();
        _staging = null;
        _duplication?.Dispose();
        _duplication = null;
        _context?.Dispose();
        _context = null;
        _device?.Dispose();
        _device = null;
        _latestBgra = null;
    }
}

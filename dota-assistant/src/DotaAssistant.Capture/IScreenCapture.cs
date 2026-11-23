namespace DotaAssistant.Capture;

/// <summary>
/// Screen capture abstraction: stats for the HUD plus optional BGRA frames for vision.
/// </summary>
public interface IScreenCapture : IAsyncDisposable
{
    CaptureStats GetStats();

    /// <summary>
    /// Returns a clone of the latest CPU pixel snapshot if available.
    /// DXGI backends copy at a throttled rate; stubs never provide pixels.
    /// </summary>
    bool TryCloneLatestPixels(out CapturePixelFrame? frame);

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync();
}

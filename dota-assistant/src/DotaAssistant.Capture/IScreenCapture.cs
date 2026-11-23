namespace DotaAssistant.Capture;

/// <summary>
/// Screen capture abstraction. V0 only exposes stats; vision consumes frames later.
/// </summary>
public interface IScreenCapture : IAsyncDisposable
{
    CaptureStats GetStats();
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync();
}

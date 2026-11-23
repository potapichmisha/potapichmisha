namespace DotaAssistant.Capture;

public static class ScreenCaptureFactory
{
    /// <summary>
    /// Prefer DXGI Desktop Duplication; fall back to stub if init fails.
    /// </summary>
    public static async Task<IScreenCapture> CreateAndStartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var dxgi = new DxgiDesktopDuplicationCapture();
            await dxgi.StartAsync(cancellationToken).ConfigureAwait(false);
            return dxgi;
        }
        catch (Exception ex)
        {
            var stub = new StubScreenCapture(ex.Message);
            await stub.StartAsync(cancellationToken).ConfigureAwait(false);
            return stub;
        }
    }
}

namespace DotaAssistant.Capture;

/// <summary>
/// CPU-side BGRA snapshot for vision. Copied from DXGI at a low rate (not every FPS tick).
/// </summary>
public sealed class CapturePixelFrame
{
    public required int Width { get; init; }
    public required int Height { get; init; }

    /// <summary>Row pitch in bytes (may be greater than Width * 4).</summary>
    public required int RowPitch { get; init; }

    /// <summary>BGRA8 bytes; length is at least RowPitch * Height.</summary>
    public required byte[] BgraPixels { get; init; }

    public required DateTimeOffset CapturedAt { get; init; }
    public required long FrameIndex { get; init; }
}

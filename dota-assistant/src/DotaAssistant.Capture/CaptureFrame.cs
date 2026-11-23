namespace DotaAssistant.Capture;

/// <summary>
/// Lightweight frame metadata for V0. Pixel buffers / ROI come in later milestones.
/// </summary>
public sealed class CaptureFrame
{
    public required DateTimeOffset CapturedAt { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required long FrameIndex { get; init; }
}

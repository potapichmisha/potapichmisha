namespace DotaAssistant.Capture;

public sealed class CaptureStats
{
    public double Fps { get; init; }
    public DateTimeOffset? LastFrameAt { get; init; }
    public long FrameCount { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Backend { get; init; } = string.Empty;
}

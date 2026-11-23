namespace DotaAssistant.Vision.Models;

/// <summary>
/// Result of recognizing the player's hero and inventory slots from one frame.
/// </summary>
public sealed class RecognitionResult
{
    public MatchCandidate? Hero { get; init; }
    public IReadOnlyList<MatchCandidate> InventoryItems { get; init; } = Array.Empty<MatchCandidate>();
    public required int FrameWidth { get; init; }
    public required int FrameHeight { get; init; }
    public required string LayoutName { get; init; }
}

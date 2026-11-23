namespace DotaAssistant.Vision.Models;

/// <summary>
/// Best template match for a single ROI slot.
/// </summary>
public sealed class MatchCandidate
{
    public required string Id { get; init; }
    public required string DisplayNameRu { get; init; }
    public required string Kind { get; init; }
    public required double Confidence { get; init; }
    public required RoiRect Roi { get; init; }
    public bool IsConfident { get; init; }
}

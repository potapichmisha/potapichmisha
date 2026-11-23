namespace DotaAssistant.Vision.Models;

/// <summary>
/// Result of recognizing top-bar heroes and inventory slots from one frame.
/// Candidates may be low-confidence guesses — check <see cref="MatchCandidate.IsConfident"/>.
/// </summary>
public sealed class RecognitionResult
{
    /// <summary>Best match for the player's top-bar icon (may be low-confidence).</summary>
    public MatchCandidate? Hero { get; init; }

    /// <summary>Other ally top-bar slots (left of center on synthetic layout).</summary>
    public IReadOnlyList<MatchCandidate> Allies { get; init; } = Array.Empty<MatchCandidate>();

    /// <summary>Enemy top-bar slots (right of center on synthetic layout).</summary>
    public IReadOnlyList<MatchCandidate> Enemies { get; init; } = Array.Empty<MatchCandidate>();

    /// <summary>Inventory slot matches (includes low-confidence guesses when a best template exists).</summary>
    public IReadOnlyList<MatchCandidate> InventoryItems { get; init; } = Array.Empty<MatchCandidate>();

    public required int FrameWidth { get; init; }
    public required int FrameHeight { get; init; }
    public required string LayoutName { get; init; }

    /// <summary>Minimum confidence used for <see cref="MatchCandidate.IsConfident"/>.</summary>
    public double MinConfidence { get; init; }

    /// <summary>True when the player hero match meets the confidence threshold.</summary>
    public bool HasConfidentHero => Hero is { IsConfident: true };

    /// <summary>Lowest confidence among confident matches, or hero guess confidence if none confident.</summary>
    public double? HeadlineConfidence
    {
        get
        {
            if (Hero is not null)
            {
                return Hero.Confidence;
            }

            var any = Allies.Concat(Enemies).Concat(InventoryItems).ToList();
            return any.Count == 0 ? null : any.Max(c => c.Confidence);
        }
    }
}

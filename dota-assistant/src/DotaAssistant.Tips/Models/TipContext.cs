namespace DotaAssistant.Tips.Models;

/// <summary>
/// Input for the tip engine: recognized (or demo) mid-lane state.
/// </summary>
public sealed class TipContext
{
    public required string HeroId { get; init; }
    public string? OpponentId { get; init; }

    /// <summary>Hero level 1–30. Use 0 when unknown so demo defaults can fill it.</summary>
    public int Level { get; init; }

    public IReadOnlyList<string> OwnedItemIds { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Optional team side (Radiant/Dire). Set by manual override when recognition has no side.
    /// Values: "radiant", "dire", or null.
    /// </summary>
    public string? SideId { get; init; }
}


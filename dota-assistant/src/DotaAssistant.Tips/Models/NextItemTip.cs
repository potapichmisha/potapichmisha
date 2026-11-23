namespace DotaAssistant.Tips.Models;

/// <summary>
/// One suggested next purchase from the curated mid build.
/// </summary>
public sealed class NextItemTip
{
    public required string ItemId { get; init; }
    public required string NameRu { get; init; }
    public required string WhyRu { get; init; }
    public required string PhraseRu { get; init; }
}

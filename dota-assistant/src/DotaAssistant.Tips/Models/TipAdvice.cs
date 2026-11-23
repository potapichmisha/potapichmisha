namespace DotaAssistant.Tips.Models;

/// <summary>
/// Full tip payload for the UI: next items + optional matchup phrase.
/// </summary>
public sealed class TipAdvice
{
    public IReadOnlyList<NextItemTip> NextItems { get; init; } = Array.Empty<NextItemTip>();
    public MatchupTip? Matchup { get; init; }
    public string? BuildStatusRu { get; init; }
    public string? MatchupStatusRu { get; init; }
    public string? DemoContextRu { get; init; }
}

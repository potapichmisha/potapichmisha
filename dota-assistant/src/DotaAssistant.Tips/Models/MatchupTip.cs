namespace DotaAssistant.Tips.Models;

/// <summary>
/// Mid-lane aggression advice from local matchup rules.
/// </summary>
public sealed class MatchupTip
{
    /// <summary>press | cautious | farm | call_support</summary>
    public required string Aggression { get; init; }
    public required string PhraseRu { get; init; }
    public required string OpponentId { get; init; }
    public required string OpponentNameRu { get; init; }
}

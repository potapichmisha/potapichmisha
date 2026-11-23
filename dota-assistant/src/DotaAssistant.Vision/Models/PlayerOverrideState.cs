namespace DotaAssistant.Vision.Models;

/// <summary>
/// Optional manual hero/side when auto-recognition is wrong.
/// Null / empty means “use auto”.
/// </summary>
public sealed class PlayerOverrideState
{
    public string? HeroId { get; set; }
    public string? HeroDisplayNameRu { get; set; }
    public PlayerSide? Side { get; set; }

    public bool HasHeroOverride => !string.IsNullOrWhiteSpace(HeroId);
    public bool HasSideOverride => Side is not null;

    public void ClearHero()
    {
        HeroId = null;
        HeroDisplayNameRu = null;
    }

    public void ClearSide() => Side = null;

    public void ClearAll()
    {
        ClearHero();
        ClearSide();
    }
}

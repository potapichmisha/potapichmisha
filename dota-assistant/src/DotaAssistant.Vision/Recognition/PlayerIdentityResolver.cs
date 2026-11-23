using DotaAssistant.Vision.Models;

namespace DotaAssistant.Vision.Recognition;

/// <summary>
/// Merges auto recognition with optional manual hero/side overrides.
/// </summary>
public static class PlayerIdentityResolver
{
    public const string ModeAutoRu = "авто";
    public const string ModeManualRu = "вручную";

    public static EffectivePlayerIdentity Resolve(
        RecognitionResult? recognition,
        PlayerOverrideState? overrideState)
    {
        overrideState ??= new PlayerOverrideState();

        string? heroId;
        string? heroName;
        double? confidence;
        bool heroManual;
        bool usable;

        if (overrideState.HasHeroOverride)
        {
            heroId = overrideState.HeroId!.Trim();
            heroName = string.IsNullOrWhiteSpace(overrideState.HeroDisplayNameRu)
                ? heroId
                : overrideState.HeroDisplayNameRu.Trim();
            confidence = null;
            heroManual = true;
            usable = true;
        }
        else
        {
            var auto = recognition?.Hero;
            heroId = auto?.Id;
            heroName = auto?.DisplayNameRu;
            confidence = auto?.Confidence;
            heroManual = false;
            usable = recognition is { HasConfidentHero: true };
        }

        PlayerSide? side;
        bool sideManual;
        if (overrideState.HasSideOverride)
        {
            side = overrideState.Side;
            sideManual = true;
        }
        else
        {
            // Auto side detection is not implemented yet.
            side = null;
            sideManual = false;
        }

        return new EffectivePlayerIdentity
        {
            HeroId = heroId,
            HeroDisplayNameRu = heroName,
            HeroConfidence = confidence,
            HeroIsManual = heroManual,
            HasUsableHero = usable,
            Side = side,
            SideIsManual = sideManual
        };
    }
}

/// <summary>
/// Effective hero + side after applying optional manual overrides.
/// </summary>
public sealed class EffectivePlayerIdentity
{
    public string? HeroId { get; init; }
    public string? HeroDisplayNameRu { get; init; }
    public double? HeroConfidence { get; init; }
    public bool HeroIsManual { get; init; }
    public bool HasUsableHero { get; init; }
    public PlayerSide? Side { get; init; }
    public bool SideIsManual { get; init; }

    public string HeroModeRu =>
        HeroIsManual ? PlayerIdentityResolver.ModeManualRu : PlayerIdentityResolver.ModeAutoRu;

    public string SideModeRu =>
        SideIsManual ? PlayerIdentityResolver.ModeManualRu : PlayerIdentityResolver.ModeAutoRu;
}

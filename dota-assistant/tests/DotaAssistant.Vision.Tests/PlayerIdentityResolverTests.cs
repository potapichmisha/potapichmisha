using DotaAssistant.Vision.Models;
using DotaAssistant.Vision.Recognition;
using Xunit;

namespace DotaAssistant.Vision.Tests;

public class PlayerIdentityResolverTests
{
    [Fact]
    public void Uses_auto_hero_when_no_override()
    {
        var recognition = MakeRecognition(
            new MatchCandidate
            {
                Id = "pudge",
                DisplayNameRu = "Pudge",
                Kind = "hero",
                Confidence = 0.91,
                Roi = new RoiRect(0, 0, 64, 64),
                IsConfident = true
            });

        var identity = PlayerIdentityResolver.Resolve(recognition, new PlayerOverrideState());

        Assert.Equal("pudge", identity.HeroId);
        Assert.Equal("Pudge", identity.HeroDisplayNameRu);
        Assert.False(identity.HeroIsManual);
        Assert.True(identity.HasUsableHero);
        Assert.Equal(PlayerIdentityResolver.ModeAutoRu, identity.HeroModeRu);
        Assert.Null(identity.Side);
        Assert.Equal(PlayerIdentityResolver.ModeAutoRu, identity.SideModeRu);
    }

    [Fact]
    public void Manual_hero_overrides_wrong_auto_match()
    {
        var recognition = MakeRecognition(
            new MatchCandidate
            {
                Id = "pudge",
                DisplayNameRu = "Pudge",
                Kind = "hero",
                Confidence = 0.95,
                Roi = new RoiRect(0, 0, 64, 64),
                IsConfident = true
            });

        var overrides = new PlayerOverrideState
        {
            HeroId = "invoker",
            HeroDisplayNameRu = "Invoker",
            Side = PlayerSide.Dire
        };

        var identity = PlayerIdentityResolver.Resolve(recognition, overrides);

        Assert.Equal("invoker", identity.HeroId);
        Assert.Equal("Invoker", identity.HeroDisplayNameRu);
        Assert.True(identity.HeroIsManual);
        Assert.True(identity.HasUsableHero);
        Assert.Null(identity.HeroConfidence);
        Assert.Equal(PlayerSide.Dire, identity.Side);
        Assert.True(identity.SideIsManual);
        Assert.Equal(PlayerIdentityResolver.ModeManualRu, identity.HeroModeRu);
        Assert.Equal(PlayerIdentityResolver.ModeManualRu, identity.SideModeRu);
    }

    [Fact]
    public void Manual_hero_is_usable_even_when_auto_is_low_confidence()
    {
        var recognition = MakeRecognition(
            new MatchCandidate
            {
                Id = "pudge",
                DisplayNameRu = "Pudge",
                Kind = "hero",
                Confidence = 0.4,
                Roi = new RoiRect(0, 0, 64, 64),
                IsConfident = false
            });

        var overrides = new PlayerOverrideState
        {
            HeroId = "queen_of_pain",
            HeroDisplayNameRu = "Queen of Pain"
        };

        var identity = PlayerIdentityResolver.Resolve(recognition, overrides);

        Assert.True(identity.HasUsableHero);
        Assert.Equal("queen_of_pain", identity.HeroId);
    }

    [Fact]
    public void Low_confidence_auto_without_override_is_not_usable()
    {
        var recognition = MakeRecognition(
            new MatchCandidate
            {
                Id = "pudge",
                DisplayNameRu = "Pudge",
                Kind = "hero",
                Confidence = 0.4,
                Roi = new RoiRect(0, 0, 64, 64),
                IsConfident = false
            });

        var identity = PlayerIdentityResolver.Resolve(recognition, null);

        Assert.False(identity.HasUsableHero);
        Assert.Equal("pudge", identity.HeroId);
        Assert.False(identity.HeroIsManual);
    }

    private static RecognitionResult MakeRecognition(MatchCandidate hero) => new()
    {
        Hero = hero,
        FrameWidth = 1920,
        FrameHeight = 1080,
        LayoutName = "test",
        MinConfidence = 0.75
    };
}

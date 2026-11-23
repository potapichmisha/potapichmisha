using DotaAssistant.Tips.Engine;
using DotaAssistant.Tips.Models;
using DotaAssistant.Tips.Rules;
using Xunit;

namespace DotaAssistant.Tips.Tests;

public class TipEngineTests
{
    private static TipEngine CreateEngine()
    {
        Assert.True(File.Exists(AssetPaths.RulesJson), AssetPaths.RulesJson);
        var store = TipRulesStore.LoadFromTipsDirectory(AssetPaths.TipsRoot);
        return new TipEngine(store);
    }

    [Fact]
    public void Loads_mid_rules_from_json()
    {
        var store = TipRulesStore.LoadFromTipsDirectory(AssetPaths.TipsRoot);
        Assert.Equal("mid", store.Document.Lane);
        Assert.Equal("1-alpha", store.Document.Version);
        Assert.NotEmpty(store.Document.Builds);
        Assert.NotEmpty(store.Document.Matchups);
        Assert.Contains("press", store.Document.Phrases.Keys);
    }

    [Fact]
    public void Suggests_next_three_items_skipping_owned()
    {
        var engine = CreateEngine();
        var advice = engine.Advise(new TipContext
        {
            HeroId = "pudge",
            OpponentId = "invoker",
            Level = 5,
            OwnedItemIds = new[] { "blink_dagger", "bottle" }
        });

        Assert.Equal(3, advice.NextItems.Count);
        Assert.Equal("boots", advice.NextItems[0].ItemId);
        Assert.Equal("magic_wand", advice.NextItems[1].ItemId);
        Assert.Equal("force_staff", advice.NextItems[2].ItemId);
        Assert.All(advice.NextItems, tip =>
        {
            Assert.False(string.IsNullOrWhiteSpace(tip.NameRu));
            Assert.False(string.IsNullOrWhiteSpace(tip.PhraseRu));
            Assert.Contains(tip.NameRu, tip.PhraseRu, StringComparison.Ordinal);
        });
        Assert.Null(advice.BuildStatusRu);
    }

    [Fact]
    public void Matchup_pudge_vs_invoker_level_5_is_cautious()
    {
        var engine = CreateEngine();
        var advice = engine.Advise(new TipContext
        {
            HeroId = "pudge",
            OpponentId = "invoker",
            Level = 5,
            OwnedItemIds = Array.Empty<string>()
        });

        Assert.NotNull(advice.Matchup);
        Assert.Equal("cautious", advice.Matchup!.Aggression);
        Assert.Contains("Invoker", advice.Matchup.PhraseRu, StringComparison.Ordinal);
        Assert.Contains("5", advice.Matchup.PhraseRu, StringComparison.Ordinal);
        Assert.Null(advice.MatchupStatusRu);
    }

    [Fact]
    public void Matchup_changes_with_level_band()
    {
        var engine = CreateEngine();
        var early = engine.Advise(new TipContext
        {
            HeroId = "pudge",
            OpponentId = "invoker",
            Level = 4,
            OwnedItemIds = Array.Empty<string>()
        });
        var late = engine.Advise(new TipContext
        {
            HeroId = "pudge",
            OpponentId = "invoker",
            Level = 14,
            OwnedItemIds = Array.Empty<string>()
        });

        Assert.Equal("cautious", early.Matchup!.Aggression);
        Assert.Equal("press", late.Matchup!.Aggression);
    }

    [Fact]
    public void EnrichWithDemoDefaults_fills_opponent_and_level()
    {
        var engine = CreateEngine();
        var enriched = engine.EnrichWithDemoDefaults(new TipContext
        {
            HeroId = "pudge",
            OwnedItemIds = new[] { "bottle" }
        });

        Assert.Equal("invoker", enriched.OpponentId);
        Assert.Equal(5, enriched.Level);

        var advice = engine.Advise(enriched);
        Assert.NotNull(advice.Matchup);
        Assert.NotEmpty(advice.NextItems);
        Assert.NotNull(advice.DemoContextRu);
    }

    [Fact]
    public void EnrichWithDemoDefaults_preserves_manual_side()
    {
        var engine = CreateEngine();
        var enriched = engine.EnrichWithDemoDefaults(new TipContext
        {
            HeroId = "invoker",
            SideId = "dire",
            OwnedItemIds = Array.Empty<string>()
        });

        Assert.Equal("dire", enriched.SideId);
        Assert.Equal("invoker", enriched.HeroId);
        Assert.NotNull(enriched.OpponentId);
    }

    [Fact]
    public void Unknown_hero_returns_build_status_without_crash()
    {
        var engine = CreateEngine();
        var advice = engine.Advise(new TipContext
        {
            HeroId = "unknown_hero",
            OpponentId = "invoker",
            Level = 3,
            OwnedItemIds = Array.Empty<string>()
        });

        Assert.Empty(advice.NextItems);
        Assert.Contains("unknown_hero", advice.BuildStatusRu!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Complete_build_reports_status()
    {
        var engine = CreateEngine();
        var advice = engine.Advise(new TipContext
        {
            HeroId = "pudge",
            OpponentId = "invoker",
            Level = 20,
            OwnedItemIds = new[]
            {
                "boots", "bottle", "magic_wand", "blink_dagger", "force_staff", "aghanims_scepter"
            }
        });

        Assert.Empty(advice.NextItems);
        Assert.Contains("куплены", advice.BuildStatusRu!, StringComparison.OrdinalIgnoreCase);
    }
}

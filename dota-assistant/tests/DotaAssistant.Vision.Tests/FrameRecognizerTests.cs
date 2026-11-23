using System.Text.Json;
using System.Text.Json.Serialization;
using DotaAssistant.Vision.Catalog;
using DotaAssistant.Vision.Recognition;
using Xunit;

namespace DotaAssistant.Vision.Tests;

public class FrameRecognizerTests
{
    [Fact]
    public void Recognizes_hero_allies_enemies_and_inventory_on_fixture()
    {
        Assert.True(File.Exists(AssetPaths.MatchHudFixture), AssetPaths.MatchHudFixture);
        Assert.True(File.Exists(AssetPaths.MatchHudExpected), AssetPaths.MatchHudExpected);

        var expected = JsonSerializer.Deserialize<ExpectedDto>(File.ReadAllText(AssetPaths.MatchHudExpected))
            ?? throw new InvalidOperationException("expected json empty");

        var catalog = TemplateCatalog.LoadFromDirectory(AssetPaths.TemplatesRoot);
        var recognizer = new FrameRecognizer(catalog, maxInventorySlots: 2);
        var result = recognizer.RecognizeFile(AssetPaths.MatchHudFixture);

        Assert.NotNull(result.Hero);
        Assert.Equal(expected.HeroId, result.Hero!.Id);
        Assert.True(result.Hero.IsConfident, $"hero confidence={result.Hero.Confidence:F3}");
        Assert.True(result.HasConfidentHero);

        Assert.Equal(expected.AllyIds.Length, result.Allies.Count);
        for (var i = 0; i < expected.AllyIds.Length; i++)
        {
            Assert.Equal(expected.AllyIds[i], result.Allies[i].Id);
            Assert.True(result.Allies[i].IsConfident, result.Allies[i].Id);
        }

        Assert.Equal(expected.EnemyIds.Length, result.Enemies.Count);
        for (var i = 0; i < expected.EnemyIds.Length; i++)
        {
            Assert.Equal(expected.EnemyIds[i], result.Enemies[i].Id);
            Assert.True(result.Enemies[i].IsConfident, result.Enemies[i].Id);
        }

        Assert.Equal(2, result.InventoryItems.Count);
        Assert.Equal(expected.InventoryIds[0], result.InventoryItems[0].Id);
        Assert.Equal(expected.InventoryIds[1], result.InventoryItems[1].Id);
        Assert.All(result.InventoryItems, item => Assert.True(item.IsConfident, item.Id));
    }

    [Fact]
    public void Does_not_confuse_pudge_with_invoker_on_fixture()
    {
        var catalog = TemplateCatalog.LoadFromDirectory(AssetPaths.TemplatesRoot);
        var recognizer = new FrameRecognizer(catalog, maxInventorySlots: 2);
        var result = recognizer.RecognizeFile(AssetPaths.MatchHudFixture);

        Assert.Equal("pudge", result.Hero!.Id);
        Assert.NotEqual("invoker", result.Hero.Id);
    }

    [Fact]
    public void Empty_dark_frame_marks_hero_not_confident()
    {
        var catalog = TemplateCatalog.LoadFromDirectory(AssetPaths.TemplatesRoot);
        var recognizer = new FrameRecognizer(catalog, minConfidence: 0.82, maxInventorySlots: 6);
        var pixels = new byte[1920 * 1080];
        var gray = new DotaAssistant.Vision.Imaging.GrayImage(1920, 1080, pixels);
        var result = recognizer.Recognize(gray);

        Assert.NotNull(result.Hero);
        Assert.False(result.Hero!.IsConfident);
        Assert.False(result.HasConfidentHero);
        Assert.All(result.Allies, a => Assert.False(a.IsConfident));
        Assert.All(result.Enemies, e => Assert.False(e.IsConfident));
    }

    private sealed class ExpectedDto
    {
        [JsonPropertyName("hero_id")]
        public string HeroId { get; set; } = "";

        [JsonPropertyName("ally_ids")]
        public string[] AllyIds { get; set; } = Array.Empty<string>();

        [JsonPropertyName("enemy_ids")]
        public string[] EnemyIds { get; set; } = Array.Empty<string>();

        [JsonPropertyName("inventory_ids")]
        public string[] InventoryIds { get; set; } = Array.Empty<string>();
    }
}

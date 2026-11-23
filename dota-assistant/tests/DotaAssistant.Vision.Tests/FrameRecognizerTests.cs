using System.Text.Json;
using System.Text.Json.Serialization;
using DotaAssistant.Vision.Catalog;
using DotaAssistant.Vision.Recognition;
using Xunit;

namespace DotaAssistant.Vision.Tests;

public class FrameRecognizerTests
{
    [Fact]
    public void Recognizes_hero_and_two_inventory_items_on_fixture()
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

        Assert.Equal(2, result.InventoryItems.Count);
        Assert.Equal(expected.InventoryIds[0], result.InventoryItems[0].Id);
        Assert.Equal(expected.InventoryIds[1], result.InventoryItems[1].Id);
        Assert.All(result.InventoryItems, item => Assert.True(item.IsConfident, item.Id));
    }

    [Fact]
    public void Does_not_confuse_pudge_with_invoker_on_fixture()
    {
        var catalog = TemplateCatalog.LoadFromDirectory(AssetPaths.TemplatesRoot);
        var recognizer = new FrameRecognizer(catalog);
        var result = recognizer.RecognizeFile(AssetPaths.MatchHudFixture);

        Assert.Equal("pudge", result.Hero!.Id);
        Assert.NotEqual("invoker", result.Hero.Id);
    }

    private sealed class ExpectedDto
    {
        [JsonPropertyName("hero_id")]
        public string HeroId { get; set; } = "";

        [JsonPropertyName("inventory_ids")]
        public string[] InventoryIds { get; set; } = Array.Empty<string>();
    }
}

using DotaAssistant.Vision.Imaging;
using DotaAssistant.Vision.Matching;
using Xunit;

namespace DotaAssistant.Vision.Tests;

public class TemplateMatcherTests
{
    private readonly NormalizedCrossCorrelationMatcher _matcher = new();

    [Fact]
    public void Identical_patches_score_near_one()
    {
        var a = ImageLoader.LoadGray(Path.Combine(AssetPaths.TemplatesRoot, "heroes", "pudge.png"));
        var b = ImageLoader.LoadGray(Path.Combine(AssetPaths.TemplatesRoot, "heroes", "pudge.png"));
        Assert.InRange(_matcher.Score(a, b), 0.99, 1.01);
    }

    [Fact]
    public void Different_heroes_score_lower_than_identical()
    {
        var pudge = ImageLoader.LoadGray(Path.Combine(AssetPaths.TemplatesRoot, "heroes", "pudge.png"));
        var invoker = ImageLoader.LoadGray(Path.Combine(AssetPaths.TemplatesRoot, "heroes", "invoker.png"));
        var same = _matcher.Score(pudge, pudge);
        var diff = _matcher.Score(pudge, invoker);
        Assert.True(same > diff + 0.15, $"same={same:F3} diff={diff:F3}");
    }
}

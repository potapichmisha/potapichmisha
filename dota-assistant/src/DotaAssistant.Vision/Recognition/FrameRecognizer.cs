using DotaAssistant.Vision.Catalog;
using DotaAssistant.Vision.Imaging;
using DotaAssistant.Vision.Layout;
using DotaAssistant.Vision.Matching;
using DotaAssistant.Vision.Models;

namespace DotaAssistant.Vision.Recognition;

/// <summary>
/// Crops HUD ROIs from a grayscale frame and matches hero + inventory templates.
/// </summary>
public sealed class FrameRecognizer
{
    private readonly TemplateCatalog _catalog;
    private readonly ITemplateMatcher _matcher;
    private readonly HudLayout _layout;
    private readonly double _minConfidence;
    private readonly int _maxInventorySlots;

    public FrameRecognizer(
        TemplateCatalog catalog,
        HudLayout? layout = null,
        ITemplateMatcher? matcher = null,
        double minConfidence = 0.82,
        int maxInventorySlots = 2)
    {
        _catalog = catalog;
        _layout = layout ?? HudLayoutPresets.SyntheticFullHd;
        _matcher = matcher ?? new NormalizedCrossCorrelationMatcher();
        _minConfidence = minConfidence;
        _maxInventorySlots = Math.Clamp(maxInventorySlots, 1, 6);
    }

    public RecognitionResult Recognize(GrayImage frame)
    {
        var layout = _layout.ScaleTo(frame.Width, frame.Height);

        var heroProbe = ImageLoader.CropGray(frame, layout.PlayerHeroTopBar);
        var hero = _matcher.BestMatch(heroProbe, layout.PlayerHeroTopBar, _catalog.Heroes, _minConfidence);

        var items = new List<MatchCandidate>();
        foreach (var slot in layout.InventorySlots.Take(_maxInventorySlots))
        {
            var probe = ImageLoader.CropGray(frame, slot);
            var match = _matcher.BestMatch(probe, slot, _catalog.Items, _minConfidence);
            if (match is not null)
            {
                items.Add(match);
            }
        }

        return new RecognitionResult
        {
            Hero = hero,
            InventoryItems = items,
            FrameWidth = frame.Width,
            FrameHeight = frame.Height,
            LayoutName = layout.Name
        };
    }

    public RecognitionResult RecognizeFile(string screenshotPath)
    {
        var frame = ImageLoader.LoadGray(screenshotPath);
        return Recognize(frame);
    }
}

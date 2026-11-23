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
        int maxInventorySlots = 6)
    {
        _catalog = catalog;
        _layout = layout ?? HudLayoutPresets.SyntheticFullHd;
        _matcher = matcher ?? new NormalizedCrossCorrelationMatcher();
        _minConfidence = minConfidence;
        _maxInventorySlots = Math.Clamp(maxInventorySlots, 1, 6);
    }

    public double MinConfidence => _minConfidence;

    public RecognitionResult Recognize(GrayImage frame)
    {
        var layout = _layout.ScaleTo(frame.Width, frame.Height);

        MatchCandidate? hero = null;
        if (TryCrop(frame, layout.PlayerHeroTopBar, out var heroProbe))
        {
            hero = _matcher.BestMatch(heroProbe, layout.PlayerHeroTopBar, _catalog.Heroes, _minConfidence);
        }

        var allies = MatchSlots(frame, layout.AllyTopBarSlots, _catalog.Heroes);
        var enemies = MatchSlots(frame, layout.EnemyTopBarSlots, _catalog.Heroes);

        var items = new List<MatchCandidate>();
        foreach (var slot in layout.InventorySlots.Take(_maxInventorySlots))
        {
            if (!TryCrop(frame, slot, out var probe))
            {
                continue;
            }

            var match = _matcher.BestMatch(probe, slot, _catalog.Items, _minConfidence);
            if (match is not null)
            {
                items.Add(match);
            }
        }

        return new RecognitionResult
        {
            Hero = hero,
            Allies = allies,
            Enemies = enemies,
            InventoryItems = items,
            FrameWidth = frame.Width,
            FrameHeight = frame.Height,
            LayoutName = layout.Name,
            MinConfidence = _minConfidence
        };
    }

    public RecognitionResult RecognizeFile(string screenshotPath)
    {
        var frame = ImageLoader.LoadGray(screenshotPath);
        return Recognize(frame);
    }

    public RecognitionResult RecognizeBgra(int width, int height, byte[] bgraPixels, int rowPitch)
    {
        var frame = ImageLoader.GrayFromBgra(width, height, bgraPixels, rowPitch);
        return Recognize(frame);
    }

    private List<MatchCandidate> MatchSlots(
        GrayImage frame,
        IReadOnlyList<RoiRect> slots,
        IEnumerable<TemplateEntry> templates)
    {
        var list = new List<MatchCandidate>(slots.Count);
        foreach (var slot in slots)
        {
            if (!TryCrop(frame, slot, out var probe))
            {
                continue;
            }

            var match = _matcher.BestMatch(probe, slot, templates, _minConfidence);
            if (match is not null)
            {
                list.Add(match);
            }
        }

        return list;
    }

    /// <summary>
    /// Skips ROIs that fall outside the frame after scaling (common on non-1080 desktops).
    /// </summary>
    private static bool TryCrop(GrayImage frame, RoiRect slot, out GrayImage probe)
    {
        if (slot.X < 0 || slot.Y < 0 || slot.Width <= 0 || slot.Height <= 0
            || slot.Right > frame.Width || slot.Bottom > frame.Height)
        {
            probe = null!;
            return false;
        }

        probe = ImageLoader.CropGray(frame, slot);
        return true;
    }
}

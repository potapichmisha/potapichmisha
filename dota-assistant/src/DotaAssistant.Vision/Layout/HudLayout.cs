using DotaAssistant.Vision.Models;

namespace DotaAssistant.Vision.Layout;

/// <summary>
/// Fixed HUD ROI map for a reference resolution. V0.5 uses synthetic Full HD fixtures;
/// real Dota HUD calibration can replace these rectangles later.
/// </summary>
public sealed class HudLayout
{
    public required string Name { get; init; }
    public required int ReferenceWidth { get; init; }
    public required int ReferenceHeight { get; init; }
    public required RoiRect PlayerHeroTopBar { get; init; }
    public required IReadOnlyList<RoiRect> InventorySlots { get; init; }

    public HudLayout ScaleTo(int frameWidth, int frameHeight)
    {
        if (frameWidth == ReferenceWidth && frameHeight == ReferenceHeight)
        {
            return this;
        }

        double sx = frameWidth / (double)ReferenceWidth;
        double sy = frameHeight / (double)ReferenceHeight;

        return new HudLayout
        {
            Name = $"{Name}@{frameWidth}x{frameHeight}",
            ReferenceWidth = frameWidth,
            ReferenceHeight = frameHeight,
            PlayerHeroTopBar = ScaleRoi(PlayerHeroTopBar, sx, sy),
            InventorySlots = InventorySlots.Select(r => ScaleRoi(r, sx, sy)).ToArray()
        };
    }

    private static RoiRect ScaleRoi(RoiRect roi, double sx, double sy)
        => new(
            (int)Math.Round(roi.X * sx),
            (int)Math.Round(roi.Y * sy),
            Math.Max(1, (int)Math.Round(roi.Width * sx)),
            Math.Max(1, (int)Math.Round(roi.Height * sy)));
}

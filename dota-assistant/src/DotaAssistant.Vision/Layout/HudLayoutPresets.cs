using DotaAssistant.Vision.Models;

namespace DotaAssistant.Vision.Layout;

public static class HudLayoutPresets
{
    /// <summary>
    /// Synthetic Full HD HUD used by V0.5 fixtures.
    /// Player hero icon: top-bar ally slot nearest center-left.
    /// Inventory: bottom-center first two slots (0 = top-left, 1 = top-middle of the 3×2 grid).
    /// </summary>
    public static HudLayout SyntheticFullHd { get; } = new()
    {
        Name = "synthetic-fullhd-1080",
        ReferenceWidth = 1920,
        ReferenceHeight = 1080,
        PlayerHeroTopBar = new RoiRect(820, 12, 64, 64),
        InventorySlots =
        [
            new RoiRect(860, 960, 64, 64),
            new RoiRect(932, 960, 64, 64),
            new RoiRect(1004, 960, 64, 64),
            new RoiRect(860, 1032, 64, 64),
            new RoiRect(932, 1032, 64, 64),
            new RoiRect(1004, 1032, 64, 64)
        ]
    };
}

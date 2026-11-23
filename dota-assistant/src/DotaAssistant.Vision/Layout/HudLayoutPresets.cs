using DotaAssistant.Vision.Models;

namespace DotaAssistant.Vision.Layout;

public static class HudLayoutPresets
{
    /// <summary>
    /// Synthetic Full HD HUD used by fixtures and live scaled capture.
    /// Player hero: top-bar ally slot nearest center-left.
    /// Allies: two radiant slots to the left of the player.
    /// Enemies: two dire slots to the right of center.
    /// Inventory: bottom-center 3×2 grid (first two slots filled in the fixture).
    /// </summary>
    public static HudLayout SyntheticFullHd { get; } = new()
    {
        Name = "synthetic-fullhd-1080",
        ReferenceWidth = 1920,
        ReferenceHeight = 1080,
        PlayerHeroTopBar = new RoiRect(820, 12, 64, 64),
        AllyTopBarSlots =
        [
            new RoiRect(740, 12, 64, 64),
            new RoiRect(660, 12, 64, 64)
        ],
        EnemyTopBarSlots =
        [
            new RoiRect(1000, 12, 64, 64),
            new RoiRect(1080, 12, 64, 64)
        ],
        InventorySlots =
        [
            new RoiRect(860, 960, 64, 64),
            new RoiRect(932, 960, 64, 64),
            new RoiRect(1004, 960, 64, 64),
            // Bottom row must fit 1080: y + 64 <= 1080 → y <= 1016
            new RoiRect(860, 1016, 64, 64),
            new RoiRect(932, 1016, 64, 64),
            new RoiRect(1004, 1016, 64, 64)
        ]
    };
}

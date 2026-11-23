namespace DotaAssistant.Vision.Models;

/// <summary>
/// Pixel rectangle in screenshot coordinates (origin top-left).
/// </summary>
public readonly record struct RoiRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;

    public void EnsureFits(int frameWidth, int frameHeight)
    {
        if (X < 0 || Y < 0 || Width <= 0 || Height <= 0
            || Right > frameWidth || Bottom > frameHeight)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RoiRect),
                $"ROI ({X},{Y},{Width}x{Height}) does not fit frame {frameWidth}x{frameHeight}.");
        }
    }
}

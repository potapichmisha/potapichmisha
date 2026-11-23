using DotaAssistant.Vision.Models;

namespace DotaAssistant.Vision.Imaging;

public static class ImageLoader
{
    public static GrayImage LoadGray(string path)
        => PngCodec.LoadRgba(path).ToGray();

    public static GrayImage LoadGrayAndResize(string path, int width, int height)
        => PngCodec.LoadRgba(path).ResizeNearest(width, height).ToGray();

    public static GrayImage CropGray(GrayImage source, RoiRect roi)
    {
        roi.EnsureFits(source.Width, source.Height);
        var pixels = new byte[roi.Width * roi.Height];
        for (var y = 0; y < roi.Height; y++)
        {
            var srcRow = ((roi.Y + y) * source.Width) + roi.X;
            Array.Copy(source.Pixels, srcRow, pixels, y * roi.Width, roi.Width);
        }

        return new GrayImage(roi.Width, roi.Height, pixels);
    }

    /// <summary>
    /// Converts a tightly packed or pitched BGRA desktop frame into grayscale for matching.
    /// </summary>
    public static GrayImage GrayFromBgra(int width, int height, byte[] bgra, int rowPitch)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (rowPitch < width * 4)
        {
            throw new ArgumentOutOfRangeException(nameof(rowPitch), "Row pitch too small for BGRA.");
        }

        if (bgra.Length < rowPitch * (height - 1) + (width * 4))
        {
            throw new ArgumentException("BGRA buffer shorter than pitch × height.", nameof(bgra));
        }

        var gray = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            var row = y * rowPitch;
            for (var x = 0; x < width; x++)
            {
                var o = row + (x * 4);
                var b = bgra[o];
                var g = bgra[o + 1];
                var r = bgra[o + 2];
                gray[(y * width) + x] = (byte)((r * 77 + g * 150 + b * 29) >> 8);
            }
        }

        return new GrayImage(width, height, gray);
    }
}

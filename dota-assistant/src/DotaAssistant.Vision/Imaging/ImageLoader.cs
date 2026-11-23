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
}

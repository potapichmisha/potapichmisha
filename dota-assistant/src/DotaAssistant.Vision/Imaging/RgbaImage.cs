namespace DotaAssistant.Vision.Imaging;

public sealed class RgbaImage
{
    public RgbaImage(int width, int height, byte[] pixels)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (pixels.Length != width * height * 4)
        {
            throw new ArgumentException($"Expected {width * height * 4} bytes, got {pixels.Length}.", nameof(pixels));
        }

        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public int Width { get; }
    public int Height { get; }
    public byte[] Pixels { get; }

    public GrayImage ToGray()
    {
        var gray = new byte[Width * Height];
        for (var i = 0; i < gray.Length; i++)
        {
            var o = i * 4;
            var r = Pixels[o];
            var g = Pixels[o + 1];
            var b = Pixels[o + 2];
            gray[i] = (byte)((r * 77 + g * 150 + b * 29) >> 8);
        }

        return new GrayImage(Width, Height, gray);
    }

    public RgbaImage ResizeNearest(int width, int height)
    {
        if (width == Width && height == Height)
        {
            return this;
        }

        var dst = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            var sy = Math.Min(Height - 1, (int)((y + 0.5) * Height / height));
            for (var x = 0; x < width; x++)
            {
                var sx = Math.Min(Width - 1, (int)((x + 0.5) * Width / width));
                var si = (sy * Width + sx) * 4;
                var di = (y * width + x) * 4;
                dst[di] = Pixels[si];
                dst[di + 1] = Pixels[si + 1];
                dst[di + 2] = Pixels[si + 2];
                dst[di + 3] = Pixels[si + 3];
            }
        }

        return new RgbaImage(width, height, dst);
    }

    public void Blit(RgbaImage source, int destX, int destY)
    {
        for (var y = 0; y < source.Height; y++)
        {
            var dy = destY + y;
            if (dy < 0 || dy >= Height)
            {
                continue;
            }

            for (var x = 0; x < source.Width; x++)
            {
                var dx = destX + x;
                if (dx < 0 || dx >= Width)
                {
                    continue;
                }

                var si = (y * source.Width + x) * 4;
                var di = (dy * Width + dx) * 4;
                Pixels[di] = source.Pixels[si];
                Pixels[di + 1] = source.Pixels[si + 1];
                Pixels[di + 2] = source.Pixels[si + 2];
                Pixels[di + 3] = source.Pixels[si + 3];
            }
        }
    }

    public static RgbaImage Solid(int width, int height, byte r, byte g, byte b)
    {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = r;
            pixels[i + 1] = g;
            pixels[i + 2] = b;
            pixels[i + 3] = 255;
        }

        return new RgbaImage(width, height, pixels);
    }
}

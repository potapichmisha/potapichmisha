using System.Buffers.Binary;
using System.IO.Compression;

namespace DotaAssistant.Vision.Imaging;

/// <summary>
/// Minimal 8-bit RGBA (color type 6) non-interlaced PNG reader/writer.
/// Enough for HUD templates and fixtures; no third-party image license needed.
/// </summary>
public static class PngCodec
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static RgbaImage LoadRgba(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return DecodeRgba(bytes);
    }

    public static RgbaImage DecodeRgba(ReadOnlySpan<byte> png)
    {
        if (png.Length < 8 || !png[..8].SequenceEqual(Signature))
        {
            throw new InvalidDataException("Not a PNG file.");
        }

        var offset = 8;
        int width = 0, height = 0;
        byte bitDepth = 0, colorType = 0;
        using var idat = new MemoryStream();

        while (offset + 12 <= png.Length)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(png[offset..]);
            offset += 4;
            if (length < 0 || offset + length + 8 > png.Length)
            {
                throw new InvalidDataException("Corrupt PNG chunk.");
            }

            var type = png.Slice(offset, 4);
            offset += 4;
            var data = png.Slice(offset, length);
            offset += length;
            offset += 4; // CRC

            if (type.SequenceEqual("IHDR"u8))
            {
                width = BinaryPrimitives.ReadInt32BigEndian(data);
                height = BinaryPrimitives.ReadInt32BigEndian(data[4..]);
                bitDepth = data[8];
                colorType = data[9];
                if (bitDepth != 8 || colorType != 6 || data[10] != 0 || data[11] != 0 || data[12] != 0)
                {
                    throw new NotSupportedException(
                        $"Only 8-bit RGBA non-interlaced PNG is supported (got depth={bitDepth}, type={colorType}).");
                }
            }
            else if (type.SequenceEqual("IDAT"u8))
            {
                idat.Write(data);
            }
            else if (type.SequenceEqual("IEND"u8))
            {
                break;
            }
        }

        if (width <= 0 || height <= 0)
        {
            throw new InvalidDataException("PNG missing IHDR.");
        }

        var inflated = InflateZlib(idat.ToArray());
        var stride = 1 + (width * 4);
        if (inflated.Length < stride * height)
        {
            throw new InvalidDataException("PNG IDAT too short.");
        }

        var rgba = new byte[width * height * 4];
        var prev = new byte[width * 4];
        var cur = new byte[width * 4];

        for (var y = 0; y < height; y++)
        {
            var rowStart = y * stride;
            var filter = inflated[rowStart];
            Buffer.BlockCopy(inflated, rowStart + 1, cur, 0, cur.Length);
            ApplyFilter(filter, cur, prev);

            Buffer.BlockCopy(cur, 0, rgba, y * width * 4, cur.Length);
            (prev, cur) = (cur, prev);
            Array.Clear(cur);
        }

        return new RgbaImage(width, height, rgba);
    }

    public static void SaveRgba(string path, RgbaImage image)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, EncodeRgba(image));
    }

    public static byte[] EncodeRgba(RgbaImage image)
    {
        var raw = new byte[(1 + image.Width * 4) * image.Height];
        for (var y = 0; y < image.Height; y++)
        {
            var dest = y * (1 + image.Width * 4);
            raw[dest] = 0; // filter None
            Buffer.BlockCopy(image.Pixels, y * image.Width * 4, raw, dest + 1, image.Width * 4);
        }

        var compressed = DeflateZlib(raw);
        Span<byte> ihdr = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr, image.Width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr[4..], image.Height);
        ihdr[8] = 8;
        ihdr[9] = 6;
        ihdr[10] = 0;
        ihdr[11] = 0;
        ihdr[12] = 0;

        using var ms = new MemoryStream();
        ms.Write(Signature);
        WriteChunk(ms, "IHDR"u8, ihdr);
        WriteChunk(ms, "IDAT"u8, compressed);
        WriteChunk(ms, "IEND"u8, ReadOnlySpan<byte>.Empty);
        return ms.ToArray();
    }

    private static void ApplyFilter(byte filter, Span<byte> cur, ReadOnlySpan<byte> prev)
    {
        switch (filter)
        {
            case 0:
                return;
            case 1: // Sub
                for (var i = 4; i < cur.Length; i++)
                {
                    cur[i] = (byte)(cur[i] + cur[i - 4]);
                }

                return;
            case 2: // Up
                for (var i = 0; i < cur.Length; i++)
                {
                    cur[i] = (byte)(cur[i] + prev[i]);
                }

                return;
            case 3: // Average
                for (var i = 0; i < cur.Length; i++)
                {
                    var left = i >= 4 ? cur[i - 4] : 0;
                    var up = prev[i];
                    cur[i] = (byte)(cur[i] + ((left + up) / 2));
                }

                return;
            case 4: // Paeth
                for (var i = 0; i < cur.Length; i++)
                {
                    var a = i >= 4 ? cur[i - 4] : (byte)0;
                    var b = prev[i];
                    var c = i >= 4 ? prev[i - 4] : (byte)0;
                    cur[i] = (byte)(cur[i] + PaethPredictor(a, b, c));
                }

                return;
            default:
                throw new NotSupportedException($"Unsupported PNG filter type {filter}.");
        }
    }

    private static byte PaethPredictor(byte a, byte b, byte c)
    {
        var p = a + b - c;
        var pa = Math.Abs(p - a);
        var pb = Math.Abs(p - b);
        var pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc)
        {
            return a;
        }

        return pb <= pc ? b : c;
    }

    private static byte[] InflateZlib(byte[] zlibData)
    {
        // Skip zlib header (2 bytes) and Adler32 trailer (4 bytes).
        if (zlibData.Length < 6)
        {
            throw new InvalidDataException("zlib stream too short.");
        }

        using var input = new MemoryStream(zlibData, 2, zlibData.Length - 6);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }

    private static byte[] DeflateZlib(byte[] raw)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(0x78);
        ms.WriteByte(0x9C);
        using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(raw);
        }

        var adler = Adler32(raw);
        Span<byte> trailer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(trailer, adler);
        ms.Write(trailer);
        return ms.ToArray();
    }

    private static uint Adler32(ReadOnlySpan<byte> data)
    {
        uint a = 1, b = 0;
        foreach (var t in data)
        {
            a = (a + t) % 65521;
            b = (b + a) % 65521;
        }

        return (b << 16) | a;
    }

    private static void WriteChunk(Stream stream, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> len = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(len, data.Length);
        stream.Write(len);
        stream.Write(type);
        stream.Write(data);
        var crc = Crc32(type, data);
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        stream.Write(crcBytes);
    }

    private static uint Crc32(ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in type)
        {
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        foreach (var b in data)
        {
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ 0xFFFFFFFF;
    }

    private static readonly uint[] CrcTable = CreateCrcTable();

    private static uint[] CreateCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }
}

using System.Buffers.Binary;

namespace TravelPlanner.Api.UniversalMethod;

public static class PhotoHelper
{
    public const int MaxBytes = 5 * 1024 * 1024;

    // Проверяем сигнатуру и размеры, но не распаковываем картинку на сервере.
    public static string? ContentType(byte[] data)
    {
        if (data.Length < 24 || data.Length > MaxBytes) return null;
        var bytes = data.AsSpan();
        if (bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            && bytes.Slice(8, 4).SequenceEqual(new byte[] { 0, 0, 0, 13 })
            && bytes.Slice(12, 4).SequenceEqual("IHDR"u8)
            && bytes[^12..].SequenceEqual(new byte[] { 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 }))
        {
            var width = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(16, 4));
            var height = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(20, 4));
            return ValidSize(width, height) ? "image/png" : null;
        }
        if (bytes[0] != 255 || bytes[1] != 216 || bytes[^2] != 255 || bytes[^1] != 217) return null;
        var offset = 2;
        while (offset + 4 < bytes.Length)
        {
            if (bytes[offset++] != 255) return null;
            while (offset < bytes.Length && bytes[offset] == 255) offset++;
            if (offset >= bytes.Length) return null;
            var marker = bytes[offset++];
            if (marker is 0xDA or 0xD9) return null;
            if (offset + 2 > bytes.Length) return null;
            var length = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset, 2));
            if (length < 2 || offset + length > bytes.Length) return null;
            if (marker is 0xC0 or 0xC1 or 0xC2)
            {
                if (length < 8) return null;
                var height = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset + 3, 2));
                var width = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset + 5, 2));
                return ValidSize(width, height) ? "image/jpeg" : null;
            }
            offset += length;
        }
        return null;
    }

    private static bool ValidSize(uint width, uint height) => width is > 0 and <= 8000
        && height is > 0 and <= 8000 && (long)width * height <= 20_000_000;
}

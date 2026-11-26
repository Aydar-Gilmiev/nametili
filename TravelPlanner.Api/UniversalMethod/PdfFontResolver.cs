using PdfSharp.Fonts;

namespace TravelPlanner.Api.UniversalMethod;

public class PdfFontResolver : IFontResolver
{
    public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic) => new("Manrope");

    public byte[] GetFont(string faceName)
    {
        using var stream = typeof(PdfFontResolver).Assembly.GetManifestResourceStream("TravelPlanner.Api.Assets.Fonts.Manrope.ttf")
            ?? throw new InvalidOperationException("В сборке отсутствует шрифт PDF.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}

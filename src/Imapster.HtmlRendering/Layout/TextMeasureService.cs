namespace Imapster.HtmlRendering.Layout;

using SkiaSharp;

public class TextMeasureService : ITextMeasureService
{
    private readonly Dictionary<(string fontFamily, float fontSize), SKTypeface> _typefaces = [];

    public float MeasureText(string text, TextStyle style)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        var typeface = GetTypeface(style);
        var font = new SKFont(typeface, style.FontSize);
        font.SubpixelText = true;
        font.Ligatures = true;

        var width = font.MeasureText(text);
        font.Dispose();

        return (float)width;
    }

    public (float width, float height) MeasureTextWithBounds(string text, TextStyle style)
    {
        if (string.IsNullOrEmpty(text))
            return (0, style.LineHeight);

        var typeface = GetTypeface(style);
        var font = new SKFont(typeface, style.FontSize);
        font.SubpixelText = true;
        font.Ligatures = true;

        var width = font.MeasureText(text);
        font.Dispose();

        return ((float)width, style.LineHeight);
    }

    public float MeasureCharacterWidth(char character, TextStyle style)
    {
        var typeface = GetTypeface(style);
        var font = new SKFont(typeface, style.FontSize);
        font.SubpixelText = true;
        font.Ligatures = true;

        var width = font.MeasureText(char.ToString(character));
        font.Dispose();

        return (float)width;
    }

    private SKTypeface GetTypeface(TextStyle style)
    {
        var key = (style.FontFamily, style.FontSize);

        if (_typefaces.TryGetValue(key, out var cachedTypeface))
            return cachedTypeface;

        var typeface = SKTypeface.FromFamilyName(style.FontFamily, GetSkiaSharpFontStyle(style));
        _typefaces[key] = typeface;

        return typeface;
    }

    private SKFontStyle GetSkiaSharpFontStyle(TextStyle style)
    {
        return new SKFontStyle(
            style.FontWeight >= 700 ? SKFontWeight.Bold : SKFontWeight.Normal,
            SKFontStyleWidth.Normal,
            style.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright
        );
    }
}
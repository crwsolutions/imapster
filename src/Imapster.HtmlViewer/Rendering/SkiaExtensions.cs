using SkiaSharp;
using System.Globalization;

namespace Imapster.HtmlViewer.Rendering;

internal static class SkiaExtensions
{
    internal static SKColor ParseColorString(this string colorString)
    {
        if (colorString.Length > 1 && colorString[0] == '#' && TryParseHex(colorString[1..], out var hexColor))
            return hexColor;

        return colorString.ToLowerInvariant() switch
        {
            "black" => SKColors.Black,
            "white" => SKColors.White,
            "red" => SKColors.Red,
            "green" => SKColors.Green,
            "blue" => SKColors.Blue,
            "yellow" => SKColors.Yellow,
            "cyan" => SKColors.Cyan,
            "magenta" => SKColors.Magenta,
            "gray" or "grey" => SKColors.Gray,
            "silver" => SKColors.Silver,
            "maroon" => SKColors.Maroon,
            "olive" => SKColors.Olive,
            "lime" => SKColors.Lime,
            "aqua" => SKColors.Aqua,
            "teal" => SKColors.Teal,
            "navy" => SKColors.Navy,
            "fuchsia" => SKColors.Fuchsia,
            "purple" => SKColors.Purple,
            _ => SKColors.Black
        };
    }

    internal static SKColor ParseColorStringOrDefault(this string? colorString) => colorString?.ParseColorString() ?? SKColors.Transparent;

    internal static SKColor ParseColorString(this Color color) => color.ToSKColor();

    internal static void DrawRect(this SKCanvas canvas, float x, float y, float width, float height, SKColor color)
    {
        using var paint = new SKPaint { Color = color };
        canvas.DrawRect(x, y, width, height, paint);
    }

    internal static void DrawLine(this SKCanvas canvas, float x1, float y1, float x2, float y2, SKColor color, float strokeWidth = 1)
    {
        using var paint = new SKPaint { Color = color, StrokeWidth = strokeWidth, IsAntialias = true };
        canvas.DrawLine(x1, y1, x2, y2, paint);
    }

    /// <summary>
    /// Parses the digits of a hex color without an exception path: #RGB shorthand is
    /// expanded, #RRGGBB and #RRGGBBAA are read directly, and anything else fails.
    /// </summary>
    private static bool TryParseHex(string hex, out SKColor color)
    {
        color = default;

        if (hex.Length == 3)
            hex = $"#{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
        else
            hex = "#" + hex;

        if (hex.Length != 7 && hex.Length != 9)
            return false;

        if (!byte.TryParse(hex[1..3], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r)) return false;
        if (!byte.TryParse(hex[3..5], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g)) return false;
        if (!byte.TryParse(hex[5..7], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b)) return false;

        var a = (byte)255;
        if (hex.Length == 9 && !byte.TryParse(hex[7..9], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out a))
            return false;

        color = new SKColor(r, g, b, a);
        return true;
    }
}
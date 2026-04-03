using Microsoft.Maui.Graphics;
using SkiaSharp;

namespace Imapster.HtmlViewer.Rendering;

public static class SkiaExtensions
{
    public static SKColor ParseColorString(this string colorString)
    {
        if (colorString.StartsWith("#"))
        {
            try
            {
                var hex = colorString.Substring(1);
                if (hex.Length == 6)
                {
                    var r = byte.Parse(hex[0..2], System.Globalization.NumberStyles.HexNumber);
                    var g = byte.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber);
                    var b = byte.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber);
                    return new SKColor(r, g, b, 255);
                }
                if (hex.Length == 8)
                {
                    var r = byte.Parse(hex[0..2], System.Globalization.NumberStyles.HexNumber);
                    var g = byte.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber);
                    var b = byte.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber);
                    var a = byte.Parse(hex[6..8], System.Globalization.NumberStyles.HexNumber);
                    return new SKColor(r, g, b, a);
                }
            }
            catch { }
        }

        return colorString.ToLowerInvariant() switch
        {
            "black" => SKColors.Black, "white" => SKColors.White,
            "red" => SKColors.Red, "green" => SKColors.Green,
            "blue" => SKColors.Blue, "yellow" => SKColors.Yellow,
            "cyan" => SKColors.Cyan, "magenta" => SKColors.Magenta,
            "gray" or "grey" => SKColors.Gray, "silver" => SKColors.Silver,
            "maroon" => SKColors.Maroon, "olive" => SKColors.Olive,
            "lime" => SKColors.Lime, "aqua" => SKColors.Aqua,
            "teal" => SKColors.Teal, "navy" => SKColors.Navy,
            "fuchsia" => SKColors.Fuchsia, "purple" => SKColors.Purple,
            _ => SKColors.Black
        };
    }

    public static SKColor ParseColorStringOrDefault(this string? colorString) => colorString?.ParseColorString() ?? SKColors.Transparent;

    public static SKColor ParseColorString(this Color color)
    {
        return new SKColor(
            (byte)(color.Red * 255),
            (byte)(color.Green * 255),
            (byte)(color.Blue * 255),
            (byte)(color.Alpha * 255)
        );
    }

    public static void Clear(this SKCanvas canvas, SKColor color) => canvas.Clear(color);

    public static void DrawRect(this SKCanvas canvas, float x, float y, float width, float height, SKColor color)
        => canvas.DrawRect(x, y, width, height, new SKPaint { Color = color });

    public static void DrawLine(this SKCanvas canvas, float x1, float y1, float x2, float y2, SKColor color, float strokeWidth = 1)
    {
        var paint = new SKPaint { Color = color, StrokeWidth = strokeWidth, IsAntialias = true };
        canvas.DrawLine(x1, y1, x2, y2, paint);
    }
}
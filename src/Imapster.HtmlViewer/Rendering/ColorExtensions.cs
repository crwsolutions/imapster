using SkiaSharp;

namespace Imapster.HtmlViewer.Rendering;

/// <summary>
/// Extension methods for color conversions between SkiaSharp and MAUI.
/// </summary>
public static class ColorExtensions
{
    /// <summary>
    /// Converts SKColor to MAUI Color.
    /// </summary>
    public static Color ToMauiColor(this SKColor color)
    {
        return Color.FromRgba(color.Red / 255.0f, color.Green / 255.0f, color.Blue / 255.0f, color.Alpha / 255.0f);
    }

    /// <summary>
    /// Converts MAUI Color to SKColor.
    /// </summary>
    public static SKColor ToSKColor(this Color color)
    {
        return new SKColor(
            (byte)(color.Red * 255),
            (byte)(color.Green * 255),
            (byte)(color.Blue * 255),
            (byte)(color.Alpha * 255)
        );
    }
}
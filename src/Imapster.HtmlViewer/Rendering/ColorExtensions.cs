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
        return new SKColor(ToByte(color.Red), ToByte(color.Green), ToByte(color.Blue), ToByte(color.Alpha));
    }

    // Truncating instead of rounding loses a channel value, e.g. 0.5 -> 127 instead of 128.
    private static byte ToByte(float component) => (byte)Math.Clamp((int)MathF.Round(component * 255f), 0, 255);
}
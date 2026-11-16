using System.Globalization;
using System.Text.RegularExpressions;

namespace Imapster.HtmlViewer.Parsing;

public static partial class StringExtensions
{
    [GeneratedRegex(@"rgba?\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)")]
    private static partial Regex ColorRegex();

    public static double ParseLength(this string value)
        => ParseLength(value.AsSpan());

    public static double ParseLength(this ReadOnlySpan<char> value)
    {
        var v = value.Trim();
        if (v.IsEmpty) return 0;

        // 'rem' has to be tested before 'em': every rem value also ends with 'em', so
        // matching 'em' first slices "1.5rem" into "1.5r" and silently drops the length.
        if (TryParseUnit(v, "rem", out var rem)) return rem * 16;
        if (TryParseUnit(v, "em", out var em)) return em * 16;
        if (TryParseUnit(v, "pt", out var pt)) return pt * 1.333;
        if (TryParseUnit(v, "px", out var px)) return px;

        // A percentage resolves against a containing box the parser does not know; the
        // layout engine handles those, so here it means "no explicit length".
        if (v[^1] == '%') return 0;

        return TryParseDouble(v, out var plainResult) ? plainResult : 0;
    }

    private static bool TryParseUnit(ReadOnlySpan<char> value, string unit, out double number)
    {
        number = 0;
        if (!value.EndsWith(unit, StringComparison.OrdinalIgnoreCase))
            return false;

        return TryParseDouble(value[..^unit.Length], out number);
    }

    private static bool TryParseDouble(ReadOnlySpan<char> value, out double number)
    {
        number = 0;
        if (value.IsEmpty) return false;

        return double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number);
    }

    public static string? ConvertColorToHex(this string color)
    {
        if (string.IsNullOrEmpty(color)) return null;

        color = color.Trim();

        if (color.StartsWith('#')) return NormalizeHex(color);

        var rgbMatch = ColorRegex().Match(color);
        if (rgbMatch.Success)
        {
            // Out-of-range components occur in real email CSS; byte.Parse would throw an
            // OverflowException and abort parsing of the whole document.
            if (byte.TryParse(rgbMatch.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var r)
                && byte.TryParse(rgbMatch.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var g)
                && byte.TryParse(rgbMatch.Groups[3].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var b))
            {
                return $"#{r:X2}{g:X2}{b:X2}";
            }

            return null;
        }

        // Legacy markup spells named colors in any case (<font color="RED">).
        return color.ToLowerInvariant() switch
        {
            "black" => "#000000",
            "white" => "#FFFFFF",
            "red" => "#FF0000",
            "green" => "#008000",
            "blue" => "#0000FF",
            "yellow" => "#FFFF00",
            "cyan" => "#00FFFF",
            "magenta" => "#FF00FF",
            "gray" or "grey" => "#808080",
            "silver" => "#C0C0C0",
            "maroon" => "#800000",
            "olive" => "#808000",
            "navy" => "#000080",
            "purple" => "#800080",
            "teal" => "#008080",
            "aqua" => "#00FFFF",
            "orange" => "#FFA500",
            _ => null
        };
    }

    /// <summary>
    /// Brings a hex color into the one shape the renderers understand: the 3-digit
    /// shorthand (#f00) is expanded, 6- and 8-digit values are passed through unchanged,
    /// and anything else counts as an unsupported color.
    /// </summary>
    internal static string? NormalizeHex(string value)
    {
        var hex = value[1..];

        return hex.Length switch
        {
            3 => $"#{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}",
            6 or 8 when IsHexDigits(hex) => value,
            _ => null
        };
    }

    private static bool IsHexDigits(string value)
    {
        if (value.Length == 0) return false;

        foreach (var c in value)
        {
            if (!char.IsAsciiHexDigit(c))
                return false;
        }

        return true;
    }
}

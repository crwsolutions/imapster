using System.Text.RegularExpressions;

namespace Imapster.HtmlViewer.Parsing;

public static partial class StringExtensions
{
    [GeneratedRegex(@"rgba?\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)")]
    private static partial Regex ColorRegex();

    public static double ParseLength(this string value)
    {
        if (string.IsNullOrEmpty(value)) return 0;

        value = value.Trim();

        if (value.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            return double.TryParse(value[..^2], out var result) ? result : 0;
        }

        if (value.EndsWith("em", StringComparison.OrdinalIgnoreCase))
        {
            return double.TryParse(value[..^2], out var result) ? result * 16 : 0;
        }

        if (value.EndsWith("rem", StringComparison.OrdinalIgnoreCase))
        {
            return double.TryParse(value[..^3], out var result) ? result * 16 : 0;
        }

        if (value.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
        {
            return double.TryParse(value[..^2], out var result) ? result * 1.333 : 0;
        }

        if (value.EndsWith("%", StringComparison.OrdinalIgnoreCase)) return 0;

        return double.TryParse(value, out var plainResult) ? plainResult : 0;
    }

    public static string? ConvertColorToHex(this string color)
    {
        if (string.IsNullOrEmpty(color)) return null;

        color = color.Trim();

        if (color.StartsWith("#")) return color;

        var rgbMatch = ColorRegex().Match(color);
        if (rgbMatch.Success)
        {
            var r = byte.Parse(rgbMatch.Groups[1].Value);
            var g = byte.Parse(rgbMatch.Groups[2].Value);
            var b = byte.Parse(rgbMatch.Groups[3].Value);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        return color switch
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
}
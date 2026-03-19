namespace Imapster.HtmlRendering.Layout;

public record TextStyle
{
    public double FontSize { get; set; } = 16.0;
    public bool IsBold { get; set; } = false;
    public bool IsItalic { get; set; } = false;
    public string? FontFamily { get; set; }
    public string? Color { get; set; }
    public string? BackgroundColor { get; set; }
    public double LineHeight { get; set; } = 1.5;
    public string? TextAlign { get; set; }
    public string? TextDecoration { get; set; }
};
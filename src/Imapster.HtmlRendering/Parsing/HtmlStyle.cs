namespace Imapster.HtmlRendering.Parsing;

public record HtmlStyle
{
    public double FontSize { get; set; } = 16.0;
    public bool IsBold { get; set; } = false;
    public bool IsItalic { get; set; } = false;
    public string? FontFamily { get; set; }
    public string? Color { get; set; }
    public string? BackgroundColor { get; set; }
    public double LineHeight { get; set; } = 1.5;
    public double MarginTop { get; set; } = 0;
    public double MarginBottom { get; set; } = 0;
    public double PaddingLeft { get; set; } = 0;
    public double PaddingRight { get; set; } = 0;
    public string? TextAlign { get; set; }
    public string? TextDecoration { get; set; }
    public string? Url { get; set; }
}
namespace Imapster.HtmlViewer.Parsing;

public sealed class HtmlNode
{
    public HtmlElementType Type { get; set; }

    public string? TagName { get; set; }

    public string TextContent { get; set; } = string.Empty;

    public HtmlStyle Style { get; set; } = new();

    public Dictionary<string, string> Attributes { get; set; } = new();

    public List<HtmlNode> Children { get; set; } = new();

    public HtmlNode? Parent { get; set; }

    public string? Href { get; set; }

    public string? Src { get; set; }

    public string? Alt { get; set; }

    public int ListItemIndex { get; set; }

    public bool IsBlockElement => IsBlockType(Type);

    public bool CanContainChildren => !IsVoidElement(Type);

    public static HtmlNode CreateText(string text) => new() { Type = HtmlElementType.Text, TextContent = text };

    public static HtmlNode CreateParagraph() => new() { Type = HtmlElementType.Paragraph, TagName = "p" };

    public static HtmlNode CreateHeading(int level) => new() { Type = HtmlElementType.Heading, TagName = $"h{level}", Attributes = { { "level", level.ToString() } } };

    public static HtmlNode CreateList(bool isOrdered) => new() { Type = isOrdered ? HtmlElementType.OrderedList : HtmlElementType.UnorderedList, TagName = isOrdered ? "ol" : "ul" };

    public static HtmlNode CreateListItem() => new() { Type = HtmlElementType.ListItem, TagName = "li" };

    public static HtmlNode CreateSpan() => new() { Type = HtmlElementType.Span, TagName = "span" };

    public static HtmlNode CreateLink() => new() { Type = HtmlElementType.Link, TagName = "a" };

    public static HtmlNode CreateImage() => new() { Type = HtmlElementType.Image, TagName = "img" };

    public static HtmlNode CreateBold() => new() { Type = HtmlElementType.Bold, TagName = "strong" };

    public static HtmlNode CreateItalic() => new() { Type = HtmlElementType.Italic, TagName = "em" };

    public static HtmlNode CreateDiv() => new() { Type = HtmlElementType.Div, TagName = "div" };

    public static HtmlNode CreatePre() => new() { Type = HtmlElementType.Pre, TagName = "pre" };

    public static HtmlNode CreateBlockquote() => new() { Type = HtmlElementType.Blockquote, TagName = "blockquote" };

    public static HtmlNode CreateLineBreak() => new() { Type = HtmlElementType.LineBreak, TagName = "br" };

    public static HtmlNode CreateHorizontalRule() => new() { Type = HtmlElementType.HorizontalRule, TagName = "hr" };

    public static HtmlNode CreateTable() => new() { Type = HtmlElementType.Table, TagName = "table" };

    public static HtmlNode CreateTableRow() => new() { Type = HtmlElementType.TableRow, TagName = "tr" };

    public static HtmlNode CreateTableCell(bool isHeader = false) => new() { Type = isHeader ? HtmlElementType.TableHeaderCell : HtmlElementType.TableCell, TagName = isHeader ? "th" : "td" };

    public static HtmlNode CreateSection() => new() { Type = HtmlElementType.Section, TagName = "section" };

    public static HtmlNode CreateArticle() => new() { Type = HtmlElementType.Article, TagName = "article" };

    public static HtmlNode CreateCode() => new() { Type = HtmlElementType.Code, TagName = "code" };

    public static HtmlNode CreateQuote() => new() { Type = HtmlElementType.Quote, TagName = "q" };

    public static HtmlNode CreateCite() => new() { Type = HtmlElementType.Cite, TagName = "cite" };

    public static HtmlNode CreateMark() => new() { Type = HtmlElementType.Mark, TagName = "mark" };

    public static HtmlNode CreateSmall() => new() { Type = HtmlElementType.Small, TagName = "small" };

    public static HtmlNode CreateSubscript() => new() { Type = HtmlElementType.Subscript, TagName = "sub" };

    public static HtmlNode CreateSuperscript() => new() { Type = HtmlElementType.Superscript, TagName = "sup" };

    public static HtmlNode CreateDel() => new() { Type = HtmlElementType.Del, TagName = "del" };

    public static HtmlNode CreateIns() => new() { Type = HtmlElementType.Ins, TagName = "ins" };

    public static HtmlNode CreateAbbr() => new() { Type = HtmlElementType.Abbrev, TagName = "abbr" };

    public static HtmlNode CreateAddress() => new() { Type = HtmlElementType.Address, TagName = "address" };

    public static HtmlNode CreateFigure() => new() { Type = HtmlElementType.Figure, TagName = "figure" };

    public static HtmlNode CreateFigcaption() => new() { Type = HtmlElementType.Figcaption, TagName = "figcaption" };

    public string GetComputedFontFamily()
    {
        if (!string.IsNullOrEmpty(Style.FontFamily))
        {
            // Map common font families to MAUI-compatible names
            var fontFamily = Style.FontFamily.ToLowerInvariant().Trim();
            return fontFamily switch
            {
                "serif" => "Serif",
                "sans-serif" => "SansSerif",
                "monospace" => "Monospace",
                "cursive" => "Cursive",
                "fantasy" => "Fantasy",
                _ => fontFamily
            };
        }

        return "SansSerif"; // Default MAUI font
    }

    public double GetComputedFontSize() => Style.FontSize;

    public string? GetComputedColor() => Style.Color;

    public string? GetComputedBackgroundColor() => Style.BackgroundColor;

    public TextAlignment GetComputedTextAlign() => Style.TextAlign;

    public TextDecoration GetComputedTextDecoration() => Style.TextDecoration;

    public (double top, double right, double bottom, double left) GetComputedMargin() => (
        Style.MarginTopSet ? Style.MarginTop : 0,
        Style.MarginRightSet ? Style.MarginRight : 0,
        Style.MarginBottomSet ? Style.MarginBottom : 0,
        Style.MarginLeftSet ? Style.MarginLeft : 0
    );

    public (double top, double right, double bottom, double left) GetComputedPadding() => (
        Style.PaddingTopSet ? Style.PaddingTop : 0,
        Style.PaddingRightSet ? Style.PaddingRight : 0,
        Style.PaddingBottomSet ? Style.PaddingBottom : 0,
        Style.PaddingLeftSet ? Style.PaddingLeft : 0
    );

    public (double top, string? topColor, double right, string? rightColor, double bottom, string? bottomColor, double left, string? leftColor) GetComputedBorder() => (
        Style.BorderTopWidthSet ? Style.BorderTopWidth : 0, Style.BorderTopColor,
        Style.BorderRightWidthSet ? Style.BorderRightWidth : 0, Style.BorderRightColor,
        Style.BorderBottomWidthSet ? Style.BorderBottomWidth : 0, Style.BorderBottomColor,
        Style.BorderLeftWidthSet ? Style.BorderLeftWidth : 0, Style.BorderLeftColor
    );

    public double? GetComputedWidth() => Style.WidthSet ? Style.Width : null;

    public double? GetComputedHeight() => Style.HeightSet ? Style.Height : null;

    public double GetComputedTextIndent() => Style.TextIndentSet ? Style.TextIndent : 0;

    public string? GetComputedDisplay() => Style.Display;

    public string? GetComputedWhiteSpace() => Style.WhiteSpace;

    public VerticalAlign GetComputedVerticalAlign() => Style.VerticalAlign;

    private static bool IsBlockType(HtmlElementType type) => type switch
    {
        HtmlElementType.Paragraph or
        HtmlElementType.Heading or
        HtmlElementType.UnorderedList or
        HtmlElementType.OrderedList or
        HtmlElementType.Div or
        HtmlElementType.Pre or
        HtmlElementType.Blockquote or
        HtmlElementType.Table or
        HtmlElementType.Section or
        HtmlElementType.Article or
        HtmlElementType.Header or
        HtmlElementType.Footer or
        HtmlElementType.Nav or
        HtmlElementType.Main or
        HtmlElementType.Figure or
        HtmlElementType.Address => true,
        _ => false
    };

    private static bool IsVoidElement(HtmlElementType type) => type switch
    {
        HtmlElementType.LineBreak or
        HtmlElementType.HorizontalRule or
        HtmlElementType.Image or
        HtmlElementType.Input => true,
        _ => false
    };
}
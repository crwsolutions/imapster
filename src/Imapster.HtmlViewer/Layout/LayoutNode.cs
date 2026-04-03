using Imapster.HtmlViewer.Parsing;

namespace Imapster.HtmlViewer.Layout;

/// <summary>
/// Represents a node in the layout tree with measured dimensions.
/// </summary>
public sealed class LayoutNode
{
    /// <summary>
    /// Gets or sets the source HTML node.
    /// </summary>
    public HtmlNode? HtmlNode { get; set; }

    /// <summary>
    /// Gets or sets the layout type.
    /// </summary>
    public LayoutType LayoutType { get; set; }

    /// <summary>
    /// Gets or sets the X position of the node.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Gets or sets the Y position of the node.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Gets or sets the width of the node.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the node.
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Gets or sets the content width (excluding margins, borders, padding).
    /// </summary>
    public double ContentWidth { get; set; }

    /// <summary>
    /// Gets or sets the content height (excluding margins, borders, padding).
    /// </summary>
    public double ContentHeight { get; set; }

    /// <summary>
    /// Gets or sets the left margin.
    /// </summary>
    public double MarginLeft { get; set; }

    /// <summary>
    /// Gets or sets the right margin.
    /// </summary>
    public double MarginRight { get; set; }

    /// <summary>
    /// Gets or sets the top margin.
    /// </summary>
    public double MarginTop { get; set; }

    /// <summary>
    /// Gets or sets the bottom margin.
    /// </summary>
    public double MarginBottom { get; set; }

    /// <summary>
    /// Gets or sets the left padding.
    /// </summary>
    public double PaddingLeft { get; set; }

    /// <summary>
    /// Gets or sets the right padding.
    /// </summary>
    public double PaddingRight { get; set; }

    /// <summary>
    /// Gets or sets the top padding.
    /// </summary>
    public double PaddingTop { get; set; }

    /// <summary>
    /// Gets or sets the bottom padding.
    /// </summary>
    public double PaddingBottom { get; set; }

    /// <summary>
    /// Gets or sets the left border width.
    /// </summary>
    public double BorderLeftWidth { get; set; }

    /// <summary>
    /// Gets or sets the right border width.
    /// </summary>
    public double BorderRightWidth { get; set; }

    /// <summary>
    /// Gets or sets the top border width.
    /// </summary>
    public double BorderTopWidth { get; set; }

    /// <summary>
    /// Gets or sets the bottom border width.
    /// </summary>
    public double BorderBottomWidth { get; set; }

    /// <summary>
    /// Gets or sets the left border color.
    /// </summary>
    public string? BorderLeftColor { get; set; }

    /// <summary>
    /// Gets or sets the right border color.
    /// </summary>
    public string? BorderRightColor { get; set; }

    /// <summary>
    /// Gets or sets the top border color.
    /// </summary>
    public string? BorderTopColor { get; set; }

    /// <summary>
    /// Gets or sets the bottom border color.
    /// </summary>
    public string? BorderBottomColor { get; set; }

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the text color.
    /// </summary>
    public string? TextColor { get; set; }

    /// <summary>
    /// Gets or sets the font family.
    /// </summary>
    public string? FontFamily { get; set; }

    /// <summary>
    /// Gets or sets the font size.
    /// </summary>
    public double FontSize { get; set; }

    /// <summary>
    /// Gets or sets whether the font is bold.
    /// </summary>
    public bool FontBold { get; set; }

    /// <summary>
    /// Gets or sets whether the font is italic.
    /// </summary>
    public bool FontItalic { get; set; }

    /// <summary>
    /// Gets or sets the text alignment.
    /// </summary>
    public Parsing.TextAlignment TextAlign { get; set; }

    /// <summary>
    /// Gets or sets the text decoration.
    /// </summary>
    public TextDecoration TextDecoration { get; set; }

    /// <summary>
    /// Gets or sets the link URL (if this is a link node).
    /// </summary>
    public string? Href { get; set; }

    /// <summary>
    /// Gets or sets the image source (if this is an image node).
    /// </summary>
    public string? Src { get; set; }

    /// <summary>
    /// Gets or sets the image alt text.
    /// </summary>
    public string? Alt { get; set; }

    /// <summary>
    /// Gets or sets the list item index (for ordered lists).
    /// </summary>
    public int ListItemIndex { get; set; }

    /// <summary>
    /// Gets or sets the list style type.
    /// </summary>
    public string? ListStyleType { get; set; }

    /// <summary>
    /// Gets or sets the list style position.
    /// </summary>
    public string? ListStylePosition { get; set; }

    /// <summary>
    /// Gets or sets the vertical alignment.
    /// </summary>
    public VerticalAlign VerticalAlign { get; set; }

    /// <summary>
    /// Gets or sets the text indent.
    /// </summary>
    public double TextIndent { get; set; }

    /// <summary>
    /// Gets or sets whether text indent is set.
    /// </summary>
    public bool TextIndentSet { get; set; }

    /// <summary>
    /// Gets or sets the child nodes.
    /// </summary>
    public List<LayoutNode> Children { get; set; } = [];

    /// <summary>
    /// Gets or sets the line boxes (for text nodes).
    /// </summary>
    public List<LineBox> LineBoxes { get; set; } = [];

    /// <summary>
    /// Gets or sets the parent layout node.
    /// </summary>
    public LayoutNode? Parent { get; set; }

    /// <summary>
    /// Gets or sets whether this node is selected.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Gets or sets the selection start character index.
    /// </summary>
    public int SelectionStart { get; set; }

    /// <summary>
    /// Gets or sets the selection end character index.
    /// </summary>
    public int SelectionEnd { get; set; }

    /// <summary>
    /// Gets or sets the selection text.
    /// </summary>
    public string? SelectionText { get; set; }

    /// <summary>
    /// Gets or sets whether width is explicitly set.
    /// </summary>
    public bool WidthSet { get; set; }
}

/// <summary>
/// Represents the layout type of a node.
/// </summary>
public enum LayoutType
{
    Block,
    Inline,
    InlineBlock,
    None
}
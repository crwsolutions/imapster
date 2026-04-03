using System.Globalization;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;

namespace Imapster.HtmlViewer.Parsing;

/// <summary>
/// Represents CSS styles applied to an HTML element.
/// </summary>
public sealed class HtmlStyle
{
    // Font properties
    public string? FontFamily { get; set; }
    public double FontSize { get; set; } = 16; // Default 16px
    public bool FontWeightBold { get; set; }
    public bool FontStyleItalic { get; set; }
    public bool FontStrikeThrough { get; set; }
    public bool FontUnderline { get; set; }

    // Text properties
    public string? Color { get; set; }
    public string? BackgroundColor { get; set; }
    public TextDecoration TextDecoration { get; set; } = TextDecoration.None;
    public TextAlignment TextAlign { get; set; } = TextAlignment.Left;
    public string? TextTransform { get; set; }
    public bool TextIndentSet { get; set; }
    public double TextIndent { get; set; }

    // Box model properties
    public bool MarginTopSet { get; set; }
    public double MarginTop { get; set; }
    public bool MarginRightSet { get; set; }
    public double MarginRight { get; set; }
    public bool MarginBottomSet { get; set; }
    public double MarginBottom { get; set; }
    public bool MarginLeftSet { get; set; }
    public double MarginLeft { get; set; }

    public bool PaddingTopSet { get; set; }
    public double PaddingTop { get; set; }
    public bool PaddingRightSet { get; set; }
    public double PaddingRight { get; set; }
    public bool PaddingBottomSet { get; set; }
    public double PaddingBottom { get; set; }
    public bool PaddingLeftSet { get; set; }
    public double PaddingLeft { get; set; }

    public bool WidthSet { get; set; }
    public double Width { get; set; }
    public bool HeightSet { get; set; }
    public double Height { get; set; }

    public bool BorderTopWidthSet { get; set; }
    public double BorderTopWidth { get; set; }
    public string? BorderTopColor { get; set; }
    public bool BorderRightWidthSet { get; set; }
    public double BorderRightWidth { get; set; }
    public string? BorderRightColor { get; set; }
    public bool BorderBottomWidthSet { get; set; }
    public double BorderBottomWidth { get; set; }
    public string? BorderBottomColor { get; set; }
    public bool BorderLeftWidthSet { get; set; }
    public double BorderLeftWidth { get; set; }
    public string? BorderLeftColor { get; set; }

    // Display and positioning
    public string? Display { get; set; }
    public string? Position { get; set; }
    public bool TopSet { get; set; }
    public double Top { get; set; }
    public bool RightSet { get; set; }
    public double Right { get; set; }
    public bool BottomSet { get; set; }
    public double Bottom { get; set; }
    public bool LeftSet { get; set; }
    public double Left { get; set; }

    // List properties
    public string? ListStyleType { get; set; }
    public bool ListStylePositionSet { get; set; }
    public string? ListStylePosition { get; set; }

    // Vertical alignment
    public VerticalAlign VerticalAlign { get; set; } = VerticalAlign.Baseline;

    // White space handling
    public string? WhiteSpace { get; set; }

    /// <summary>
    /// Creates a copy of this style.
    /// </summary>
    public HtmlStyle Clone()
    {
        return new HtmlStyle
        {
            FontFamily = FontFamily,
            FontSize = FontSize,
            FontWeightBold = FontWeightBold,
            FontStyleItalic = FontStyleItalic,
            FontStrikeThrough = FontStrikeThrough,
            FontUnderline = FontUnderline,
            Color = Color,
            BackgroundColor = BackgroundColor,
            TextDecoration = TextDecoration,
            TextAlign = TextAlign,
            TextTransform = TextTransform,
            TextIndentSet = TextIndentSet,
            TextIndent = TextIndent,
            MarginTopSet = MarginTopSet,
            MarginTop = MarginTop,
            MarginRightSet = MarginRightSet,
            MarginRight = MarginRight,
            MarginBottomSet = MarginBottomSet,
            MarginBottom = MarginBottom,
            MarginLeftSet = MarginLeftSet,
            MarginLeft = MarginLeft,
            PaddingTopSet = PaddingTopSet,
            PaddingTop = PaddingTop,
            PaddingRightSet = PaddingRightSet,
            PaddingRight = PaddingRight,
            PaddingBottomSet = PaddingBottomSet,
            PaddingBottom = PaddingBottom,
            PaddingLeftSet = PaddingLeftSet,
            PaddingLeft = PaddingLeft,
            WidthSet = WidthSet,
            Width = Width,
            HeightSet = HeightSet,
            Height = Height,
            BorderTopWidthSet = BorderTopWidthSet,
            BorderTopWidth = BorderTopWidth,
            BorderTopColor = BorderTopColor,
            BorderRightWidthSet = BorderRightWidthSet,
            BorderRightWidth = BorderRightWidth,
            BorderRightColor = BorderRightColor,
            BorderBottomWidthSet = BorderBottomWidthSet,
            BorderBottomWidth = BorderBottomWidth,
            BorderBottomColor = BorderBottomColor,
            BorderLeftWidthSet = BorderLeftWidthSet,
            BorderLeftWidth = BorderLeftWidth,
            BorderLeftColor = BorderLeftColor,
            Display = Display,
            Position = Position,
            TopSet = TopSet,
            Top = Top,
            RightSet = RightSet,
            Right = Right,
            BottomSet = BottomSet,
            Bottom = Bottom,
            LeftSet = LeftSet,
            Left = Left,
            ListStyleType = ListStyleType,
            ListStylePositionSet = ListStylePositionSet,
            ListStylePosition = ListStylePosition,
            VerticalAlign = VerticalAlign,
            WhiteSpace = WhiteSpace
        };
    }

    /// <summary>
    /// Merges this style with a parent style, with this style's values taking precedence.
    /// </summary>
    public HtmlStyle MergeWith(HtmlStyle? parent)
    {
        var result = parent?.Clone() ?? new HtmlStyle();

        if (FontFamily is not null) result.FontFamily = FontFamily;
        if (FontSize != 16) result.FontSize = FontSize;
        if (FontWeightBold) result.FontWeightBold = true;
        if (FontStyleItalic) result.FontStyleItalic = true;
        if (FontStrikeThrough) result.FontStrikeThrough = true;
        if (FontUnderline) result.FontUnderline = true;
        if (Color is not null) result.Color = Color;
        if (BackgroundColor is not null) result.BackgroundColor = BackgroundColor;
        if (TextDecoration != TextDecoration.None) result.TextDecoration = TextDecoration;
        if (TextAlign != TextAlignment.Left) result.TextAlign = TextAlign;
        if (TextTransform is not null) result.TextTransform = TextTransform;
        if (TextIndentSet) { result.TextIndentSet = true; result.TextIndent = TextIndent; }
        if (MarginTopSet) { result.MarginTopSet = true; result.MarginTop = MarginTop; }
        if (MarginRightSet) { result.MarginRightSet = true; result.MarginRight = MarginRight; }
        if (MarginBottomSet) { result.MarginBottomSet = true; result.MarginBottom = MarginBottom; }
        if (MarginLeftSet) { result.MarginLeftSet = true; result.MarginLeft = MarginLeft; }
        if (PaddingTopSet) { result.PaddingTopSet = true; result.PaddingTop = PaddingTop; }
        if (PaddingRightSet) { result.PaddingRightSet = true; result.PaddingRight = PaddingRight; }
        if (PaddingBottomSet) { result.PaddingBottomSet = true; result.PaddingBottom = PaddingBottom; }
        if (PaddingLeftSet) { result.PaddingLeftSet = true; result.PaddingLeft = PaddingLeft; }
        if (WidthSet) { result.WidthSet = true; result.Width = Width; }
        if (HeightSet) { result.HeightSet = true; result.Height = Height; }
        if (BorderTopWidthSet) { result.BorderTopWidthSet = true; result.BorderTopWidth = BorderTopWidth; result.BorderTopColor = BorderTopColor; }
        if (BorderRightWidthSet) { result.BorderRightWidthSet = true; result.BorderRightWidth = BorderRightWidth; result.BorderRightColor = BorderRightColor; }
        if (BorderBottomWidthSet) { result.BorderBottomWidthSet = true; result.BorderBottomWidth = BorderBottomWidth; result.BorderBottomColor = BorderBottomColor; }
        if (BorderLeftWidthSet) { result.BorderLeftWidthSet = true; result.BorderLeftWidth = BorderLeftWidth; result.BorderLeftColor = BorderLeftColor; }
        if (Display is not null) result.Display = Display;
        if (Position is not null) result.Position = Position;
        if (TopSet) { result.TopSet = true; result.Top = Top; }
        if (RightSet) { result.RightSet = true; result.Right = Right; }
        if (BottomSet) { result.BottomSet = true; result.Bottom = Bottom; }
        if (LeftSet) { result.LeftSet = true; result.Left = Left; }
        if (ListStyleType is not null) result.ListStyleType = ListStyleType;
        if (ListStylePositionSet) { result.ListStylePositionSet = true; result.ListStylePosition = ListStylePosition; }
        if (VerticalAlign != VerticalAlign.Baseline) result.VerticalAlign = VerticalAlign;
        if (WhiteSpace is not null) result.WhiteSpace = WhiteSpace;

        return result;
    }
}

/// <summary>
/// Text alignment options.
/// </summary>
public enum TextAlignment
{
    Left,
    Right,
    Center,
    Justify
}

/// <summary>
/// Vertical alignment options.
/// </summary>
public enum VerticalAlign
{
    Baseline,
    Sub,
    Super,
    Top,
    TextTop,
    Middle,
    Bottom,
    TextBottom
}

/// <summary>
/// Text decoration options.
/// </summary>
public enum TextDecoration
{
    None,
    Underline,
    LineThrough,
    Blink
}
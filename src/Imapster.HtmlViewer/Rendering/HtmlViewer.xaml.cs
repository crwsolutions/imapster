using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using SkiaSharp;
using System.Diagnostics;
using SKPaintSurfaceEventArgs = SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs;

namespace Imapster.HtmlViewer.Rendering;

/// <summary>
/// SkiaSharp-based renderer for HTML content.
/// </summary>
public partial class HtmlViewer : ContentView
{
    private readonly LayoutEngine _layoutEngine;
    private readonly RenderContext _renderContext;
    private readonly HtmlParser _htmlParser;
    private LayoutNode? _layoutRoot;
    private HtmlNode? _htmlRoot;
    private double _lastMeasuredWidth = -1;
    private double _lastRenderedWidth = -1;

    public static readonly BindableProperty HtmlProperty = BindableProperty.Create(
        nameof(Html),
        typeof(string),
        typeof(HtmlViewer),
        null,
        propertyChanged: OnHtmlChanged);

    public string? Html
    {
        get => (string?)GetValue(HtmlProperty);
        set => SetValue(HtmlProperty, value);
    }

    private static void OnHtmlChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is HtmlViewer view && newValue is string html)
        {
            view._renderContext.HtmlContent = html;
            view.ParseAndLayout();
        }
    }

    public Color TextColor
    {
        get => _renderContext.TextColor;
        set => _renderContext.TextColor = value;
    }

    public Color LinkColor
    {
        get => _renderContext.LinkColor;
        set => _renderContext.LinkColor = value;
    }

    public double FontSize
    {
        get => _renderContext.FontSize;
        set
        {
            if (_renderContext.FontSize != value)
            {
                _renderContext.FontSize = value;
            }
        }
    }

    public string FontFamily
    {
        get => _renderContext.FontFamily;
        set
        {
            if (_renderContext.FontFamily != value)
            {
                _renderContext.FontFamily = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets whether text selection is enabled.
    /// </summary>
    public bool IsSelectionEnabled
    {
        get => _renderContext.IsSelectionEnabled;
        set => _renderContext.IsSelectionEnabled = value;
    }

    /// <summary>
    /// Gets or sets whether links are enabled.
    /// </summary>
    public bool IsLinksEnabled
    {
        get => _renderContext.IsLinksEnabled;
        set => _renderContext.IsLinksEnabled = value;
    }

    /// <summary>
    /// Gets or sets the selection start position.
    /// </summary>
    public int SelectionStart
    {
        get => _renderContext.SelectionRange?.Start ?? 0;
        set
        {
            var end = _renderContext.SelectionRange?.End ?? 0;
            _renderContext.SelectionRange = (value, end);
            InvalidateRender();
        }
    }

    /// <summary>
    /// Gets or sets the selection end position.
    /// </summary>
    public int SelectionEnd
    {
        get => _renderContext.SelectionRange?.End ?? 0;
        set
        {
            var start = _renderContext.SelectionRange?.Start ?? 0;
            _renderContext.SelectionRange = (start, value);
            InvalidateRender();
        }
    }

    /// <summary>
    /// Occurs when text is selected.
    /// </summary>
    public event EventHandler<string>? TextSelected;

    /// <summary>
    /// Occurs when a link is clicked.
    /// </summary>
    public event EventHandler<string>? LinkClicked;

    /// <summary>
    /// Creates a new instance of the HtmlViewer.
    /// </summary>
    public HtmlViewer()
    {
        InitializeComponent();

        MinimumHeightRequest = 10;
        MinimumWidthRequest = 10;

        _layoutEngine = new LayoutEngine();
        _renderContext = new RenderContext();
        _htmlParser = new HtmlParser();

        BackgroundColor = Colors.White;
        TextColor = Colors.Black;
        LinkColor = Colors.Blue;
        FontSize = 16;
        FontFamily = "Arial";
    }

    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        Debug.WriteLine($"MeasureOverride called with widthConstraint: {widthConstraint}, heightConstraint: {heightConstraint}");

        double desiredHeight = MinimumHeightRequest;

        // Check if width constraint has changed
        if (Math.Abs(widthConstraint - _lastMeasuredWidth) > 0.1)
        {
            _lastMeasuredWidth = widthConstraint;
        }

        // Only perform layout if we have valid width and content
        if (!string.IsNullOrEmpty(_renderContext.HtmlContent) && widthConstraint != double.PositiveInfinity && widthConstraint > 0)
        {
            try
            {
                _htmlRoot = _htmlParser.Parse(_renderContext.HtmlContent);
                _layoutRoot = _layoutEngine.Layout(_htmlRoot, (float)widthConstraint);
                _lastRenderedWidth = widthConstraint;
            }
            catch { }
        }

        if (_layoutRoot != null)
            desiredHeight = _layoutRoot.Height;

        desiredHeight += Margin.Top + Margin.Bottom;

        return new Size(widthConstraint, desiredHeight);
    }

    private void ParseAndLayout()
    {
        if (string.IsNullOrEmpty(_renderContext.HtmlContent))
        {
            _htmlRoot = null;
            _layoutRoot = null;
            return;
        }

        InvalidateRender();
    }

    private void InvalidateRender()
    {
        Dispatcher.Dispatch(() => _canvas?.InvalidateSurface());
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        if (e.Surface == null)
            return;

        Debug.WriteLine($"OnPaintSurface called with canvas size: {e.Surface.Canvas.DeviceClipBounds.Width}x{e.Surface.Canvas.DeviceClipBounds.Height}");

        // Get the actual rendered width from bounds
        var currentWidth = Bounds.Width;

        // Validate currentWidth - it should be > 0
        if (currentWidth <= 0)
        {
            // If Bounds.Width is invalid, use canvas width as fallback
            currentWidth = e.Surface.Canvas.DeviceClipBounds.Width > 0
                ? e.Surface.Canvas.DeviceClipBounds.Width
                : 800;
        }

        // Check if width has changed since last render or layout is dirty
        var needsLayout = Math.Abs(currentWidth - _lastRenderedWidth) > 0.1;

        if (needsLayout && !string.IsNullOrEmpty(_renderContext.HtmlContent) && currentWidth > 0)
        {
            _lastRenderedWidth = currentWidth;

            try
            {
                _htmlRoot = _htmlParser.Parse(_renderContext.HtmlContent);
                _layoutRoot = _layoutEngine.Layout(_htmlRoot, (float)currentWidth);
            }
            catch { }
        }

        // Always render - either with existing layout or clear canvas
        Render(e.Surface.Canvas);

    }

    private string GetTextFromHtmlNode(HtmlNode node, int start, int end)
    {
        if (node.Type == HtmlElementType.Text)
        {
            var text = node.TextContent ?? string.Empty;
            return (start >= 0 && end <= text.Length) ? text.Substring(start, end - start) : text;
        }

        var result = new System.Text.StringBuilder();
        foreach (var child in node.Children)
            result.Append(GetTextFromHtmlNode(child, start, end));

        return result.ToString();
    }

    public string GetSelectedText()
    {
        if (_renderContext.SelectionRange == null || _htmlRoot == null)
            return string.Empty;

        var (start, end) = _renderContext.SelectionRange.Value;
        return (start == -1 || end == -1) ? string.Empty : GetTextFromHtmlNode(_htmlRoot, start, end);
    }

    public void ClearSelection()
    {
        SelectionStart = -1;
        SelectionEnd = -1;
        _renderContext.SelectionRange = null;
        InvalidateRender();
    }

    private void Render(SKCanvas canvas)
    {
        canvas.Clear(BackgroundColor.ParseColorString());
        if (_layoutRoot == null)
            return;

        RenderNode(canvas, _layoutRoot, 0, 0);
    }

    private void RenderNode(SKCanvas canvas, LayoutNode node, double offsetX, double offsetY)
    {
        if (node.BackgroundColor is not null)
        {
            var bgX = (float)(offsetX + node.BorderLeftWidth);
            var bgY = (float)(offsetY + node.BorderTopWidth);
            var bgWidth = (float)(node.Width - node.BorderLeftWidth - node.BorderRightWidth);
            var bgHeight = (float)(node.Height - node.BorderTopWidth - node.BorderBottomWidth);

            if (bgWidth > 0 && bgHeight > 0)
                canvas.DrawRect(bgX, bgY, bgWidth, bgHeight, node.BackgroundColor.ParseColorString());
        }

        DrawBorders(canvas, offsetX, offsetY, node);

        if (node.HtmlNode?.Type == HtmlElementType.ListItem)
            RenderListMarker(canvas, node, offsetX, offsetY);

        var hasInlineChildren = node.Children.Any(c => c.LayoutType == LayoutType.Inline);
        var contentOffsetX = offsetX + node.PaddingLeft + node.BorderLeftWidth;
        var contentOffsetY = offsetY + node.PaddingTop + node.BorderTopWidth;
        var isListItem = node.HtmlNode?.Type == HtmlElementType.ListItem;
        var markerWidthOffset = isListItem ? node.FontSize * 1.5 : 0;

        if (hasInlineChildren)
        {
            foreach (var child in node.Children)
            {
                if (child.LayoutType == LayoutType.Block)
                    RenderNode(canvas, child, offsetX + child.X, offsetY + child.Y);
                else if (child.LayoutType == LayoutType.Inline)
                    RenderInlineNode(canvas, child, contentOffsetX + markerWidthOffset + child.X, contentOffsetY + child.Y);
                else if (child.LayoutType == LayoutType.None && child.HtmlNode?.Type == HtmlElementType.Text)
                    RenderTextNode(canvas, child, contentOffsetX + markerWidthOffset + child.X, contentOffsetY + child.Y, node);
            }
        }
        else
        {
            var hasNodeLineBoxes = node.LineBoxes.Count > 0;

            foreach (var child in node.Children)
            {
                if (child.LayoutType == LayoutType.Block)
                    RenderNode(canvas, child, offsetX + child.X, offsetY + child.Y);
                else if (child.LayoutType == LayoutType.None && child.HtmlNode?.Type == HtmlElementType.Text)
                {
                    // Only render text if the node doesn't have its own LineBoxes
                    // (text might be in parent's LineBoxes for block elements like lists)
                    if (!hasNodeLineBoxes)
                        RenderTextNode(canvas, child, contentOffsetX + markerWidthOffset + child.X, contentOffsetY + child.Y, node);
                }
            }
        }

        if (!hasInlineChildren && node.LineBoxes.Count > 0)
        {
            // For list items, the LineBoxes are at Y=0, so we need to add the contentOffsetY
            // For other nodes, the LineBoxes already have the correct Y position
            var yBase = isListItem ? contentOffsetY : offsetY;
            // For list items, also add markerWidthOffset to X position so text appears after the marker
            var xBase = contentOffsetX + (isListItem ? markerWidthOffset : 0);
            foreach (var line in node.LineBoxes)
                RenderLine(canvas, line, xBase + line.X, yBase + line.Y, node);
        }
    }

    private void RenderInlineNode(SKCanvas canvas, LayoutNode node, double offsetX, double offsetY)
    {
        if (node.LineBoxes.Count > 0)
        {
            foreach (var line in node.LineBoxes)
                RenderLine(canvas, line, offsetX + line.X, offsetY + line.Y, node);
        }
        else
        {
            foreach (var child in node.Children)
            {
                if (child.LayoutType == LayoutType.Inline)
                    RenderInlineNode(canvas, child, offsetX + child.X, offsetY + child.Y);
                else if (child.LayoutType == LayoutType.None && child.HtmlNode?.Type == HtmlElementType.Text)
                    RenderTextNode(canvas, child, offsetX + child.X, offsetY + child.Y, node);
            }
        }
    }

    private void RenderTextNode(SKCanvas canvas, LayoutNode node, double offsetX, double offsetY, LayoutNode? parentNode = null)
    {
        foreach (var line in node.LineBoxes)
        {
            var styleNode = parentNode ?? node;
            RenderLine(canvas, line, offsetX + line.X, offsetY + line.Y, styleNode);
        }
    }

    private void RenderLine(SKCanvas canvas, LineBox line, double x, double y, LayoutNode node)
    {
        if (string.IsNullOrEmpty(line.Text))
            return;

        var fontFamily = node.FontFamily ?? _renderContext.FontFamily;
        var fontWeight = node.FontBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
        var fontSlant = node.FontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
        var typeface = SKTypeface.FromFamilyName(fontFamily, fontWeight, SKFontStyleWidth.Normal, fontSlant);
        using var font = new SKFont(typeface) { Size = (float)node.FontSize };

        var paint = new SKPaint { IsAntialias = true };
        paint.Color = node.TextColor?.ParseColorString() ?? _renderContext.TextColor.ParseColorString();

        if (node.Href is not null && _renderContext.IsLinksEnabled)
            paint.Color = _renderContext.LinkColor.ParseColorString();

        canvas.DrawText(line.Text, (float)x, (float)(y + line.Baseline), SKTextAlign.Left, font, paint);

        if (node.TextDecoration != TextDecoration.None)
        {
            var decorationY = node.TextDecoration switch
            {
                TextDecoration.Underline => (float)(y + line.Baseline + node.FontSize * 0.1),
                TextDecoration.LineThrough => (float)(y + line.Baseline - node.FontSize * 0.2),
                _ => (float)(y + line.Baseline + node.FontSize * 0.1)
            };

            canvas.DrawLine((float)x, decorationY, (float)(x + line.Width), decorationY, paint.Color);
        }

        if (node.Href is not null && _renderContext.IsLinksEnabled && node.TextDecoration == TextDecoration.None)
        {
            var underlineY = (float)(y + line.Baseline + node.FontSize * 0.1);
            canvas.DrawLine((float)x, underlineY, (float)(x + line.Width), underlineY, _renderContext.LinkColor.ParseColorString());
        }

        if (_renderContext.IsSelectionEnabled && _renderContext.SelectionRange.HasValue)
        {
            var (start, end) = _renderContext.SelectionRange.Value;
            var lineStart = line.StartCharIndex;
            var lineEnd = line.EndCharIndex;

            if (start < lineEnd && end > lineStart)
            {
                var selectStart = Math.Max(start, lineStart);
                var selectEnd = Math.Min(end, lineEnd);
                var selectLength = selectEnd - selectStart;

                if (selectLength > 0)
                {
                    var selectionText = line.Text.Substring(selectStart - lineStart, selectLength);
                    var selectionWidth = font.MeasureText(selectionText);
                    var selectionY = y - (float)(node.FontSize * 0.25f);

                    canvas.DrawRect((float)x, (float)selectionY, (float)selectionWidth, (float)(node.FontSize * 1.25f), _renderContext.SelectionColor.ParseColorString());
                    canvas.DrawText(selectionText, (float)x, (float)(y + line.Baseline), SKTextAlign.Left, font, paint);
                }
            }
        }
    }

    private void RenderListMarker(SKCanvas canvas, LayoutNode node, double offsetX, double offsetY)
    {
        var markerX = offsetX + node.PaddingLeft;
        var markerY = offsetY + node.PaddingTop + node.FontSize;

        using var font = new SKFont(SKTypeface.Default) { Size = (float)node.FontSize };
        var paint = new SKPaint { IsAntialias = true, Color = _renderContext.TextColor.ParseColorString() };

        var listStyle = node.ListStyleType?.ToLowerInvariant() ?? "disc";
        var markerText = listStyle switch
        {
            "disc" => "•",
            "circle" => "◦",
            "square" => "◼",
            "decimal" => $"{node.ListItemIndex}.",
            "lower-roman" => ToLowerRoman(node.ListItemIndex),
            "upper-roman" => ToUpperRoman(node.ListItemIndex),
            "lower-alpha" => ToLowerAlpha(node.ListItemIndex),
            "upper-alpha" => ToUpperAlpha(node.ListItemIndex),
            _ => "•"
        };

        canvas.DrawText(markerText, (float)markerX, (float)markerY, SKTextAlign.Left, font, paint);
    }

    private string ToLowerRoman(int num) => ToUpperRoman(num).ToLowerInvariant();

    private string ToUpperRoman(int num)
    {
        if (num <= 0 || num > 3999)
            return num.ToString();

        var romanNumerals = new[] { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
        var values = new[] { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
        var result = new System.Text.StringBuilder();

        for (var i = 0; i < values.Length; i++)
        {
            while (num >= values[i])
            {
                result.Append(romanNumerals[i]);
                num -= values[i];
            }
        }

        return result.ToString();
    }

    private string ToLowerAlpha(int num) => ToUpperAlpha(num).ToLowerInvariant();

    private string ToUpperAlpha(int num)
    {
        if (num <= 0)
            return "A";

        num--;
        var result = new System.Text.StringBuilder();

        while (num >= 0)
        {
            result.Insert(0, (char)('A' + (num % 26)));
            num = (num / 26) - 1;
        }

        return result.ToString();
    }

    private void DrawBorders(SKCanvas canvas, double offsetX, double offsetY, LayoutNode node)
    {
        var x = (float)(offsetX + node.PaddingLeft);
        var y = (float)(offsetY + node.PaddingTop);
        var width = (float)(node.ContentWidth);
        var height = (float)(node.ContentHeight);

        if (node.BorderLeftWidth > 0 && node.BorderLeftColor is not null)
        {
            var paint = new SKPaint { Color = node.BorderLeftColor.ParseColorString(), StrokeWidth = (float)node.BorderLeftWidth, IsAntialias = true };
            canvas.DrawLine(x, y, x, y + height, paint);
        }

        if (node.BorderRightWidth > 0 && node.BorderRightColor is not null)
        {
            var paint = new SKPaint { Color = node.BorderRightColor.ParseColorString(), StrokeWidth = (float)node.BorderRightWidth, IsAntialias = true };
            canvas.DrawLine(x + width, y, x + width, y + height, paint);
        }

        if (node.BorderTopWidth > 0 && node.BorderTopColor is not null)
        {
            var paint = new SKPaint { Color = node.BorderTopColor.ParseColorString(), StrokeWidth = (float)node.BorderTopWidth, IsAntialias = true };
            canvas.DrawLine(x, y, x + width, y, paint);
        }

        if (node.BorderBottomWidth > 0 && node.BorderBottomColor is not null)
        {
            var paint = new SKPaint { Color = node.BorderBottomColor.ParseColorString(), StrokeWidth = (float)node.BorderBottomWidth, IsAntialias = true };
            canvas.DrawLine(x, y + height, x + width, y + height, paint);
        }
    }
}

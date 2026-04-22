using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;
using System.Diagnostics;
using SKPaintSurfaceEventArgs = SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs;

namespace Imapster.HtmlViewer.Rendering;

/// <summary>
/// SkiaSharp-based renderer for HTML content.
/// </summary>
public partial class HtmlViewer : SKCanvasView
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
        BindingMode.TwoWay,
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
            InvalidateSurface();
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
            InvalidateSurface();
        }
    }

    /// <summary>
    /// Occurs when text is selected.
    /// </summary>
#pragma warning disable CS0067 // Event is never used
    public event EventHandler<string>? TextSelected;

    /// <summary>
    /// Occurs when a link is clicked.
    /// </summary>
    public event EventHandler<string>? LinkClicked;
#pragma warning restore CS0067

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

    /// <summary>
    /// Called when the size of this element changes.
    /// Ensures the canvas is repainted when the viewer is resized.
    /// </summary>
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        Debug.WriteLine($"OnSizeAllocated called with width: {width}, height: {height}");

        InvalidateSurface();
    }

    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        Debug.WriteLine($"MeasureOverride called with widthConstraint: {widthConstraint}, heightConstraint: {heightConstraint}");

        // Check if width constraint has changed
        bool widthChanged = Math.Abs(widthConstraint - _lastMeasuredWidth) > 0.1;
        if (widthChanged)
        {
            _lastMeasuredWidth = widthConstraint;
        }

        // Only perform layout if width has changed AND we have valid width and content
        // If HTML changed but width is the same, ParseAndLayout() already calculated the layout
        if (widthChanged && !string.IsNullOrEmpty(_renderContext.HtmlContent) && widthConstraint != double.PositiveInfinity && widthConstraint > 0)
        {
            try
            {
                _htmlRoot = _htmlParser.Parse(_renderContext.HtmlContent);
                _layoutRoot = _layoutEngine.Layout(_htmlRoot, (float)widthConstraint);
                _lastRenderedWidth = widthConstraint;
            }
            catch { }
        }

        var desiredHeight = GetDesiredHeight();
        HeightRequest = desiredHeight;
        return new Size(widthConstraint, desiredHeight);
    }

    private double GetDesiredHeight()
    {
        double desiredHeight = MinimumHeightRequest;

        if (_layoutRoot != null)
        {
            desiredHeight = _layoutRoot.Height;
        }

        desiredHeight += Margin.Top + Margin.Bottom;

        return desiredHeight;
    }

    private void ParseAndLayout()
    {
        if (string.IsNullOrEmpty(_renderContext.HtmlContent))
        {
            _htmlRoot = null;
            _layoutRoot = null;
            HeightRequest = -1;
        }
        else
        {
            _htmlRoot = _htmlParser.Parse(_renderContext.HtmlContent);
            if (_lastRenderedWidth > 0)
            {
                _layoutRoot = _layoutEngine.Layout(_htmlRoot, (float)_lastRenderedWidth);
            }
        }

        HeightRequest = GetDesiredHeight();
        if (_layoutRoot is not null && Parent is Grid parent)
        {
            parent.HeightRequest = HeightRequest;
        }
        InvalidateSurface();
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
            return (start >= 0 && end <= text.Length) ? text[start..end] : text;
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
        InvalidateSurface();
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
        // Render background and borders
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

        // Render list marker if applicable
        if (node.HtmlNode?.Type == HtmlElementType.ListItem)
            RenderListMarker(canvas, node, offsetX, offsetY);

        // Calculate content offsets
        var contentOffsetX = offsetX + node.PaddingLeft + node.BorderLeftWidth;
        var contentOffsetY = offsetY + node.PaddingTop + node.BorderTopWidth;
        var isListItem = node.HtmlNode?.Type == HtmlElementType.ListItem;
        var markerWidthOffset = isListItem ? node.FontSize * 1.5 : 0;

        // Render block children first (they are positioned absolutely)
        foreach (var child in node.Children)
        {
            if (child.LayoutType == LayoutType.Block)
            {
                RenderNode(canvas, child, offsetX + child.X, offsetY + child.Y);
            }
        }

        // Render inline content: recurse through inline children until we reach text nodes or LineBoxes
        RenderInlineContent(canvas, node, contentOffsetX + markerWidthOffset, contentOffsetY);
    }

    /// <summary>
    /// Finds a child LineBox that matches the parent LineBox based on text content and X position.
    /// Returns (child, lineBox) if found, null otherwise.
    /// </summary>
    private (LayoutNode, LineBox)? FindChildLineBox(LayoutNode node, LineBox parentLineBox)
    {
        // First try: exact match by text and X position
        foreach (var child in node.Children)
        {
            if (child.LayoutType == LayoutType.Inline && child.LineBoxes.Count > 0)
            {
                foreach (var lineBox in child.LineBoxes)
                {
                    if (lineBox.Text == parentLineBox.Text &&
                        Math.Abs((child.X + lineBox.X) - parentLineBox.X) < 0.01)
                    {
                        return (child, lineBox);
                    }
                }
            }
        }

        // Second try: match by text only (in case X position is slightly off due to layout)
        foreach (var child in node.Children)
        {
            if (child.LayoutType == LayoutType.Inline && child.LineBoxes.Count > 0)
            {
                foreach (var lineBox in child.LineBoxes)
                {
                    if (lineBox.Text == parentLineBox.Text && !string.IsNullOrEmpty(lineBox.Text))
                    {
                        return (child, lineBox);
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Renders inline content. Renders LineBoxes if the node has them, otherwise recurses into children.
    /// When rendering parent LineBoxes, we use them for positioning but render children for styling.
    /// </summary>
    private void RenderInlineContent(SKCanvas canvas, LayoutNode node, double offsetX, double offsetY)
    {
        // If this node has LineBoxes (it's a leaf or contains inline text that was laid out), render them
        if (node.LineBoxes.Count > 0)
        {
            // Check if any children have their own LineBoxes (inline elements like <strong>, <a>, etc.)
            var hasInlineChildrenWithLineBoxes = node.Children.Any(c =>
                c.LayoutType == LayoutType.Inline && c.LineBoxes.Count > 0);

            if (hasInlineChildrenWithLineBoxes)
            {
                // Parent has LineBoxes (including empty ones from <br/>), but children also have LineBoxes for styling
                // For each parent LineBox, check if a child covers it. If yes, render child (styled). If no, render parent (plain/empty).
                foreach (var parentLineBox in node.LineBoxes)
                {
                    // Find child LineBox matching this parent LineBox by text and X position
                    var childLineBox = FindChildLineBox(node, parentLineBox);

                    if (childLineBox != null)
                    {
                        // Render child LineBox (styled text) using parent's Y position
                        var (child, cb) = childLineBox.Value;
                        RenderLine(canvas, cb, offsetX + parentLineBox.X, offsetY + parentLineBox.Y, child);
                    }
                    else
                    {
                        // No matching child - render parent (plain text or empty line from <br/>)
                        RenderLine(canvas, parentLineBox, offsetX + parentLineBox.X, offsetY + parentLineBox.Y, node);
                    }
                }
            }
            else
            {
                // No inline children with their own LineBoxes - render all parent LineBoxes
                foreach (var lineBox in node.LineBoxes)
                {
                    RenderLine(canvas, lineBox, offsetX + lineBox.X, offsetY + lineBox.Y, node);
                }
            }
        }
        else
        {
            // Otherwise recurse into children (container node)
            foreach (var child in node.Children)
            {
                if (child.LayoutType == LayoutType.Inline)
                {
                    // Recurse into inline child with its offset
                    RenderInlineContent(canvas, child, offsetX + child.X, offsetY + child.Y);
                }
                else if (child.LayoutType == LayoutType.None && child.HtmlNode?.Type == HtmlElementType.Text)
                {
                    // Text node - it should have LineBoxes from the parent's layout, but if not, skip
                    // (Text nodes don't have their own LineBoxes; they're part of the parent's inline flow)
                }
            }
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
        // Note: We don't skip empty lines - they still reserve vertical space for proper layout

        if (!string.IsNullOrEmpty(line.Text))
        {
            var fontFamily = node.FontFamily ?? _renderContext.FontFamily;
            var fontWeight = node.FontBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
            var fontSlant = node.FontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
            var typeface = SKTypeface.FromFamilyName(fontFamily, fontWeight, SKFontStyleWidth.Normal, fontSlant);
            using var font = new SKFont(typeface) { Size = (float)node.FontSize };

            // Render text with per-span styling using StyleSpans
            if (line.StyleSpans.Count > 0)
            {
                if (line.Text.Contains("fbto"))
                {
                    System.Diagnostics.Debug.WriteLine($"RenderLine FBTO: text='{line.Text}' spans={line.StyleSpans.Count}");
                    foreach (var span in line.StyleSpans.OrderBy(s => s.StartIndex))
                    {
                        System.Diagnostics.Debug.WriteLine($"  span: start={span.StartIndex} len={span.Length} text='{line.Text.Substring(span.StartIndex, span.Length)}' href={span.SourceNode?.Href}");
                    }
                }

                // Sort spans by StartIndex to ensure correct rendering order
                var sortedSpans = line.StyleSpans.OrderBy(s => s.StartIndex).ToList();

                var currentX = x;
                var textIndex = 0;

                foreach (var span in sortedSpans)
                {
                    // Validate span bounds
                    if (span.StartIndex < 0 || span.StartIndex > line.Text.Length)
                        continue;
                    if (span.StartIndex + span.Length > line.Text.Length)
                        continue;

                    // Render gap before this span with unstyled text
                    if (textIndex < span.StartIndex)
                    {
                        var gapLength = span.StartIndex - textIndex;
                        var gapText = line.Text.Substring(textIndex, gapLength);
                        var gapPaint = new SKPaint
                        {
                            IsAntialias = true,
                            Color = node.TextColor?.ParseColorString() ?? _renderContext.TextColor.ParseColorString()
                        };
                        canvas.DrawText(gapText, (float)currentX, (float)(y + line.Baseline), SKTextAlign.Left, font, gapPaint);
                        currentX += font.MeasureText(gapText);
                        textIndex = span.StartIndex;
                    }

                    // Render the styled span
                    var spanText = line.Text.Substring(span.StartIndex, span.Length);
                    var sourceNode = span.SourceNode ?? node;

                    var paint = new SKPaint
                    {
                        IsAntialias = true,
                        Color = sourceNode.TextColor?.ParseColorString() ?? _renderContext.TextColor.ParseColorString()
                    };

                    // Apply link color if source node has href
                    if (sourceNode.Href is not null && _renderContext.IsLinksEnabled)
                    {
                        paint.Color = _renderContext.LinkColor.ParseColorString();
                    }

                    canvas.DrawText(spanText, (float)currentX, (float)(y + line.Baseline), SKTextAlign.Left, font, paint);

                    // Draw decorations
                    var spanWidth = font.MeasureText(spanText);

                    if (sourceNode.TextDecoration != TextDecoration.None)
                    {
                        var decorationY = sourceNode.TextDecoration switch
                        {
                            TextDecoration.Underline => (float)(y + line.Baseline + node.FontSize * 0.1),
                            TextDecoration.LineThrough => (float)(y + line.Baseline - node.FontSize * 0.2),
                            _ => (float)(y + line.Baseline + node.FontSize * 0.1)
                        };

                        canvas.DrawLine((float)currentX, decorationY, (float)(currentX + spanWidth), decorationY, paint.Color);
                    }

                    // Add underline for links if no text decoration
                    if (sourceNode.Href is not null && _renderContext.IsLinksEnabled && sourceNode.TextDecoration == TextDecoration.None)
                    {
                        var underlineY = (float)(y + line.Baseline + node.FontSize * 0.1);
                        canvas.DrawLine((float)currentX, underlineY, (float)(currentX + spanWidth), underlineY, _renderContext.LinkColor.ParseColorString());
                    }

                    currentX += spanWidth;
                    textIndex = span.StartIndex + span.Length;
                }

                // Render remaining text after last span
                if (textIndex < line.Text.Length)
                {
                    var remainingText = line.Text.Substring(textIndex);
                    var remainingPaint = new SKPaint
                    {
                        IsAntialias = true,
                        Color = node.TextColor?.ParseColorString() ?? _renderContext.TextColor.ParseColorString()
                    };
                    canvas.DrawText(remainingText, (float)currentX, (float)(y + line.Baseline), SKTextAlign.Left, font, remainingPaint);
                }
            }
            else
            {
                // No style spans - render entire line with node styling
                var isLinkNode = node.Href is not null;

                var paint = new SKPaint
                {
                    IsAntialias = true,
                    Color = node.TextColor?.ParseColorString() ?? _renderContext.TextColor.ParseColorString()
                };

                if (isLinkNode && _renderContext.IsLinksEnabled)
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

                if (isLinkNode && _renderContext.IsLinksEnabled && node.TextDecoration == TextDecoration.None)
                {
                    var underlineY = (float)(y + line.Baseline + node.FontSize * 0.1);
                    canvas.DrawLine((float)x, underlineY, (float)(x + line.Width), underlineY, _renderContext.LinkColor.ParseColorString());
                }
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

                    if (selectLength > 0 && selectStart - lineStart < line.Text.Length)
                    {
                        var selectionText = line.Text.Substring(selectStart - lineStart, Math.Min(selectLength, line.Text.Length - (selectStart - lineStart)));
                        var selectionWidth = font.MeasureText(selectionText);
                        var selectionY = y - (float)(node.FontSize * 0.25f);

                        var selectionPaint = new SKPaint
                        {
                            IsAntialias = true,
                            Color = node.TextColor?.ParseColorString() ?? _renderContext.TextColor.ParseColorString()
                        };

                        canvas.DrawRect((float)x, (float)selectionY, (float)selectionWidth, (float)(node.FontSize * 1.25f), _renderContext.SelectionColor.ParseColorString());
                        canvas.DrawText(selectionText, (float)x, (float)(y + line.Baseline), SKTextAlign.Left, font, selectionPaint);
                    }
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

using Imapster.HtmlViewer.Parsing;
using SkiaSharp;
using System.Text;

namespace Imapster.HtmlViewer.Layout;

/// <summary>
/// Engine for calculating layout of parsed HTML content.
/// </summary>
public sealed class LayoutEngine
{
    /// <summary>
    /// Key identifying a unique font style for caching purposes.
    /// </summary>
    private readonly record struct FontStyleKey(string Family, SKFontStyleWeight Weight, SKFontStyleSlant Slant);

    /// <summary>
    /// Key identifying a unique line metrics query for caching purposes.
    /// </summary>
    private readonly record struct LineMetricsKey(string Family, double FontSize, bool Bold, bool Italic);

    private readonly SKTypeface _defaultTypeface;
    private readonly double _defaultFontSize;
    private readonly string _defaultFontFamily;
    private readonly Dictionary<FontStyleKey, SKTypeface> _typefaceCache = new();
    private readonly Dictionary<LineMetricsKey, (double Ascent, double Descent, double Baseline, double LineHeight)> _lineMetricsCache = new();

    // Default margins for block elements (in em units, relative to font size)
    private const double DefaultParagraphMargin = 1.0;
    private const double DefaultHeadingMarginTop = 0.67;
    private const double DefaultHeadingMarginBottom = 0.67;
    private const double DefaultBlockquoteMargin = 1.0;
    private const double DefaultListMargin = 1.0;
    private const double DefaultListItemPadding = 0.5;
    private const double DefaultMarkerWidth = 2.5; // Width for list markers in em (bullet + space)
    private const double DefaultTableCellPadding = 4; // Default padding for td/th cells (px)

    /// <summary>
    /// Gets the font line metrics (ascent, descent, baseline, line height) for a node's font.
    /// Line height is ascent + descent + 2 and baseline is ascent + 1, so text sits near the
    /// vertical center of the line. Falls back to fontSize * 1.2 / fontSize * 0.8 when the
    /// typeface metrics are unusable (e.g. ascent <= 0). The result is cached per
    /// (family, size, bold, italic) so repeated spans share a single font query.
    /// </summary>
    private (double Ascent, double Descent, double Baseline, double LineHeight) GetLineMetrics(double fontSize, string? fontFamily, bool fontBold, bool fontItalic)
    {
        if (fontSize <= 0)
            return (0, 0, 0, 0);

        var key = new LineMetricsKey(fontFamily ?? _defaultFontFamily, fontSize, fontBold, fontItalic);
        if (_lineMetricsCache.TryGetValue(key, out var cached))
            return cached;

        var typeface = GetOrCreateTypeface(fontFamily ?? _defaultFontFamily,
            fontBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            fontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);

        using var font = new SKFont(typeface) { Size = (float)fontSize };

        var metrics = font.Metrics;
        double ascent = metrics.Ascent < 0 ? -metrics.Ascent : 0;
        double descent = metrics.Descent > 0 ? metrics.Descent : 0;

        if (ascent <= 0)
        {
            ascent = fontSize * 0.8;
            descent = fontSize * 0.2;
        }

        var lineHeight = ascent + descent + 2;
        var baseline = ascent + 1;
        var result = (ascent, descent, baseline, lineHeight);
        _lineMetricsCache[key] = result;
        return result;
    }

    /// <summary>
    /// Creates a new instance of the LayoutEngine.
    /// </summary>
    public LayoutEngine()
    {
        _defaultTypeface = SKTypeface.Default;
        _defaultFontSize = 16;
        _defaultFontFamily = "Arial";
    }

    /// <summary>
    /// Creates a new instance of the LayoutEngine with custom defaults.
    /// </summary>
    public LayoutEngine(double defaultFontSize, string defaultFontFamily)
    {
        _defaultFontSize = defaultFontSize;
        _defaultFontFamily = defaultFontFamily;
        _defaultTypeface = SKTypeface.FromFamilyName(defaultFontFamily, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
    }

    /// <summary>
    /// Disposes of the typeface cache.
    /// </summary>
    public void Dispose()
    {
        foreach (var typeface in _typefaceCache.Values)
        {
            typeface.Dispose();
        }
        _typefaceCache.Clear();

        // SKTypeface.Default is shared and must stay alive; the parameterized constructor
        // creates its own instance, and that one has to be released.
        if (!ReferenceEquals(_defaultTypeface, SKTypeface.Default))
            _defaultTypeface.Dispose();
    }

    /// <summary>
    /// Gets the default list style type for a list item.
    /// Returns the provided styleType if set, otherwise returns the default based on parent list type.
    /// </summary>
    private string? GetDefaultListStyleType(HtmlElementType elementType, HtmlNode? htmlNode, string? styleType)
    {
        // If a style type is explicitly set, use it
        if (!string.IsNullOrEmpty(styleType))
            return styleType;

        // For list items, check if parent is an ordered list
        if (elementType == HtmlElementType.ListItem && htmlNode?.Parent != null)
        {
            // Default to decimal for ordered lists, disc for unordered lists
            return htmlNode.Parent.Type == HtmlElementType.OrderedList ? "decimal" : "disc";
        }

        return styleType;
    }

    /// <summary>
    /// Layouts the given HTML node tree.
    /// </summary>
    /// <param name="htmlNode">The root HTML node.</param>
    /// <param name="availableWidth">The available width for layout.</param>
    /// <returns>The layout tree root node.</returns>
    public LayoutNode Layout(HtmlNode htmlNode, double availableWidth)
    {
        var layoutNode = ConvertHtmlNode(htmlNode, null, availableWidth);
        return layoutNode;
    }

    /// <summary>
    /// Gets whether this node has already been laid out for the given width. Layout propagates
    /// the resolved content width while converting the tree, so a node normally reaches layout
    /// exactly once; the guard keeps a containing block from laying out a subtree repeatedly,
    /// which made layout cost grow with the nesting depth of the email tables.
    /// </summary>
    private static bool IsLaidOutAt(LayoutNode node, double availableWidth) =>
        !double.IsNaN(node.LaidOutWidth) && Math.Abs(node.LaidOutWidth - availableWidth) < 0.01;

    private static void MarkLaidOut(LayoutNode node, double availableWidth) =>
        node.LaidOutWidth = availableWidth;

    /// <summary>
    /// Resolves the node's border-box width and the width its children lay out in. A fixed
    /// width larger than the containing block is clamped to it, so nested fixed-width tables
    /// (common in marketing emails) never overflow their parent cell.
    /// </summary>
    private static double ResolveInnerWidth(LayoutNode node, double availableWidth)
    {
        if (node.WidthSet && node.Width > availableWidth)
            node.Width = availableWidth;

        var contentWidth = node.WidthSet ? node.Width : availableWidth - node.MarginLeft - node.MarginRight;

        // Calculate content width accounting for padding and borders
        var innerWidth = contentWidth - node.PaddingLeft - node.PaddingRight - node.BorderLeftWidth - node.BorderRightWidth;

        node.Width = contentWidth;
        node.ContentWidth = innerWidth;
        return innerWidth;
    }

    /// <summary>
    /// Converts an HTML node to a layout node.
    /// </summary>
    private LayoutNode ConvertHtmlNode(HtmlNode htmlNode, HtmlStyle? parentStyle, double availableWidth)
    {
        var style = htmlNode.Style ?? new HtmlStyle();
        var mergedStyle = style.MergeWith(parentStyle);

        var layoutNode = new LayoutNode
        {
            HtmlNode = htmlNode,
            LayoutType = GetLayoutType(htmlNode.Type),
            TextColor = mergedStyle.Color,
            BackgroundColor = mergedStyle.BackgroundColor,
            FontFamily = mergedStyle.FontFamily ?? _defaultFontFamily,
            FontSize = mergedStyle.FontSize > 0 ? mergedStyle.FontSize : _defaultFontSize,
            FontBold = mergedStyle.FontWeightBold,
            FontItalic = mergedStyle.FontStyleItalic,
            TextAlign = mergedStyle.TextAlign,
            TextDecoration = mergedStyle.TextDecoration,
            VerticalAlign = mergedStyle.VerticalAlign,
            TextIndentSet = mergedStyle.TextIndentSet,
            TextIndent = mergedStyle.TextIndent,
            Href = htmlNode.Href,
            Src = htmlNode.Src,
            Alt = htmlNode.Alt,
            ListItemIndex = htmlNode.ListItemIndex,
            ListStyleType = GetDefaultListStyleType(htmlNode.Type, htmlNode, mergedStyle.ListStyleType),
            ListStylePosition = mergedStyle.ListStylePosition,
            MarginLeft = mergedStyle.MarginLeftSet ? mergedStyle.MarginLeft : 0,
            MarginRight = mergedStyle.MarginRightSet ? mergedStyle.MarginRight : 0,
            MarginTop = mergedStyle.MarginTopSet ? mergedStyle.MarginTop : 0,
            MarginBottom = mergedStyle.MarginBottomSet ? mergedStyle.MarginBottom : 0,
            PaddingLeft = mergedStyle.PaddingLeftSet ? mergedStyle.PaddingLeft : 0,
            PaddingRight = mergedStyle.PaddingRightSet ? mergedStyle.PaddingRight : 0,
            PaddingTop = mergedStyle.PaddingTopSet ? mergedStyle.PaddingTop : 0,
            PaddingBottom = mergedStyle.PaddingBottomSet ? mergedStyle.PaddingBottom : 0,
            BorderLeftWidth = mergedStyle.BorderLeftWidthSet ? mergedStyle.BorderLeftWidth : 0,
            BorderRightWidth = mergedStyle.BorderRightWidthSet ? mergedStyle.BorderRightWidth : 0,
            BorderTopWidth = mergedStyle.BorderTopWidthSet ? mergedStyle.BorderTopWidth : 0,
            BorderBottomWidth = mergedStyle.BorderBottomWidthSet ? mergedStyle.BorderBottomWidth : 0,
            BorderLeftColor = mergedStyle.BorderLeftColor,
            BorderRightColor = mergedStyle.BorderRightColor,
            BorderTopColor = mergedStyle.BorderTopColor,
            BorderBottomColor = mergedStyle.BorderBottomColor,
            Width = mergedStyle.WidthSet ? mergedStyle.Width : 0,
            WidthSet = mergedStyle.WidthSet
        };

        // When the CSS width is not set, fall back to the legacy HTML width attribute,
        // which is commonly used on tables in marketing emails (e.g. <table width="640">).
        // Percentage values are resolved against the available width.
        if (!layoutNode.WidthSet &&
            htmlNode.Type is HtmlElementType.Table or HtmlElementType.TableCell or HtmlElementType.TableHeaderCell &&
            htmlNode.Attributes.TryGetValue("width", out var widthAttribute) &&
            TryParseLegacyWidth(widthAttribute, availableWidth, out var attributeWidth))
        {
            layoutNode.Width = attributeWidth;
            layoutNode.WidthSet = true;
        }

        // Apply default margins for block elements if not explicitly set
        ApplyDefaultMargins(layoutNode, htmlNode.Type, mergedStyle.FontSize);

        // Convert children with the width they will actually be laid out in: their containing
        // block's content width for block elements, the inherited width for inline content.
        // Resolving it here (instead of letting the containing block relayout the subtree at
        // this width afterwards) is what keeps every node to a single layout pass.
        var childWidth = layoutNode.LayoutType == LayoutType.Block
            ? ResolveInnerWidth(layoutNode, availableWidth)
            : availableWidth;

        // Process children
        foreach (var child in htmlNode.Children)
        {
            var childLayoutNode = ConvertHtmlNode(child, mergedStyle, childWidth);
            childLayoutNode.Parent = layoutNode;
            layoutNode.Children.Add(childLayoutNode);
        }

        // Layout the node
        if (layoutNode.LayoutType == LayoutType.Block)
        {
            if (htmlNode.Type == HtmlElementType.TableRow)
                LayoutTableRow(layoutNode, availableWidth);
            else
                LayoutBlockNode(layoutNode, availableWidth);
        }
        else if (layoutNode.LayoutType == LayoutType.Inline)
        {
            LayoutInlineNode(layoutNode, availableWidth);
        }
        else if (layoutNode.LayoutType == LayoutType.None && layoutNode.HtmlNode?.Type == HtmlElementType.Text)
        {
            // Handle text nodes directly
            LayoutTextNode(layoutNode, availableWidth);
        }
        else if (layoutNode.HtmlNode?.Type == HtmlElementType.LineBreak)
        {
            // BR elements create a line break with minimal height (just line height)
            var brMetrics = GetLineMetrics(layoutNode.FontSize, layoutNode.FontFamily, layoutNode.FontBold, layoutNode.FontItalic);
            layoutNode.Height = brMetrics.LineHeight;
            layoutNode.Width = 0;
        }
        else if (layoutNode.LayoutType == LayoutType.None && layoutNode.Children.Count > 0)
        {
            // Unknown/deprecated elements (e.g. the legacy <font> wrapper used by many
            // emails) have no explicit layout type. Without this their content is never
            // laid out or rendered. Mirror CSS blockification: treat the element as a
            // block when it contains block-level children, otherwise as inline.
            layoutNode.LayoutType = layoutNode.Children.Any(c => c.LayoutType == LayoutType.Block)
                ? LayoutType.Block
                : LayoutType.Inline;

            if (layoutNode.LayoutType == LayoutType.Block)
                LayoutBlockNode(layoutNode, availableWidth);
            else
                LayoutInlineNode(layoutNode, availableWidth);
        }

        return layoutNode;
    }

    /// <summary>
    /// Applies default margins for block elements.
    /// </summary>
    private void ApplyDefaultMargins(LayoutNode node, HtmlElementType type, double fontSize)
    {
        if (node.LayoutType != LayoutType.Block)
            return;

        // Only apply defaults if margins are not explicitly set
        switch (type)
        {
            case HtmlElementType.Paragraph:
                if (!node.HtmlNode!.Style.MarginTopSet)
                    node.MarginTop = fontSize * DefaultParagraphMargin;
                if (!node.HtmlNode!.Style.MarginBottomSet)
                    node.MarginBottom = fontSize * DefaultParagraphMargin;
                break;

            case HtmlElementType.Heading:
                if (!node.HtmlNode!.Style.MarginTopSet)
                    node.MarginTop = fontSize * DefaultHeadingMarginTop;
                if (!node.HtmlNode!.Style.MarginBottomSet)
                    node.MarginBottom = fontSize * DefaultHeadingMarginBottom;
                break;

            case HtmlElementType.Blockquote:
                if (!node.HtmlNode!.Style.MarginLeftSet)
                    node.MarginLeft = fontSize * DefaultBlockquoteMargin;
                if (!node.HtmlNode!.Style.MarginRightSet)
                    node.MarginRight = fontSize * DefaultBlockquoteMargin;
                if (!node.HtmlNode!.Style.MarginTopSet)
                    node.MarginTop = fontSize * DefaultBlockquoteMargin;
                if (!node.HtmlNode!.Style.MarginBottomSet)
                    node.MarginBottom = fontSize * DefaultBlockquoteMargin;
                break;

            case HtmlElementType.UnorderedList:
            case HtmlElementType.OrderedList:
                if (!node.HtmlNode!.Style.MarginLeftSet)
                    node.MarginLeft = fontSize * DefaultListMargin;
                if (!node.HtmlNode!.Style.MarginRightSet)
                    node.MarginRight = fontSize * DefaultListMargin;
                if (!node.HtmlNode!.Style.MarginTopSet)
                    node.MarginTop = fontSize * DefaultListMargin;
                if (!node.HtmlNode!.Style.MarginBottomSet)
                    node.MarginBottom = fontSize * DefaultListMargin;
                break;

            case HtmlElementType.ListItem:
                // Add padding for list markers
                if (!node.HtmlNode!.Style.PaddingLeftSet)
                    node.PaddingLeft = fontSize * DefaultMarkerWidth;
                if (!node.HtmlNode!.Style.PaddingTopSet)
                    node.PaddingTop = fontSize * DefaultListItemPadding;
                if (!node.HtmlNode!.Style.PaddingBottomSet)
                    node.PaddingBottom = fontSize * DefaultListItemPadding;
                break;

            case HtmlElementType.Pre:
                // Pre blocks typically have some margin
                if (!node.HtmlNode!.Style.MarginTopSet)
                    node.MarginTop = fontSize * 0.5;
                if (!node.HtmlNode!.Style.MarginBottomSet)
                    node.MarginBottom = fontSize * 0.5;
                if (!node.HtmlNode!.Style.PaddingLeftSet)
                    node.PaddingLeft = fontSize * 0.5;
                if (!node.HtmlNode!.Style.PaddingRightSet)
                    node.PaddingRight = fontSize * 0.5;
                break;
        }
    }

    /// <summary>
    /// Gets the layout type for an HTML element type.
    /// </summary>
    private LayoutType GetLayoutType(HtmlElementType elementType)
    {
        return elementType switch
        {
            HtmlElementType.Div or
            HtmlElementType.Paragraph or
            HtmlElementType.Heading or
            HtmlElementType.UnorderedList or
            HtmlElementType.OrderedList or
            HtmlElementType.ListItem or
            HtmlElementType.Blockquote or
            HtmlElementType.Pre or
            HtmlElementType.HorizontalRule or
            HtmlElementType.Section or
            HtmlElementType.Article or
            HtmlElementType.Header or
            HtmlElementType.Footer or
            HtmlElementType.Nav or
            HtmlElementType.Main or
            HtmlElementType.Center or
            HtmlElementType.Table or
            HtmlElementType.TableHeader or
            HtmlElementType.TableBody or
            HtmlElementType.TableFooter or
            HtmlElementType.TableRow or
            HtmlElementType.TableCell or
            HtmlElementType.TableHeaderCell or
            HtmlElementType.Figure or
            HtmlElementType.Address or
            HtmlElementType.DocumentFragment => LayoutType.Block,

            HtmlElementType.Span or
            HtmlElementType.Link or
            HtmlElementType.Bold or
            HtmlElementType.Italic or
            HtmlElementType.Underline or
            HtmlElementType.Code or
            HtmlElementType.Quote or
            HtmlElementType.Cite or
            HtmlElementType.Mark or
            HtmlElementType.Small or
            HtmlElementType.Subscript or
            HtmlElementType.Superscript or
            HtmlElementType.Del or
            HtmlElementType.Ins or
            HtmlElementType.Abbrev or
            HtmlElementType.Figcaption => LayoutType.Inline,

            HtmlElementType.Image => LayoutType.Inline,

            _ => LayoutType.None
        };
    }

    /// <summary>
    /// Layouts a block-level node.
    /// </summary>
    private void LayoutBlockNode(LayoutNode node, double availableWidth)
    {
        // ConvertHtmlNode already laid out this node's children at the width this block
        // resolves for them, so reaching the same width again must not repeat the work.
        if (IsLaidOutAt(node, availableWidth))
            return;

        node.LineBoxes.Clear();

        var innerWidth = ResolveInnerWidth(node, availableWidth);
        var contentWidth = node.Width;

        // Lay out the children in document order. Block children are laid out as they are
        // encountered; consecutive inline children are grouped into runs so that inline
        // content placed after a block (e.g. the closing text after the last paragraph of a
        // legacy <font>-wrapped email) is positioned below that block instead of at the
        // top of the container.
        var totalHeight = 0.0;
        var currentY = 0.0;
        var inlineRun = new List<LayoutNode>();

        void FinishInlineRun(double y)
        {
            if (inlineRun.Count == 0)
                return;

            // Collect all inline content (text and inline elements) to create unified line breaking
            var inlineContent = CollectInlineContent(inlineRun);

            if (inlineContent.HasContent)
            {
                // Create line boxes that wrap all inline content together. Style spans (and the
                // line height/baseline) are built from the source node of each character.
                var lines = BreakTextIntoLines(inlineContent.CombinedText, node, innerWidth, inlineRun);

                // Distribute inline elements across lines based on text positions
                LayoutInlineChildrenOnLines(inlineRun, lines, node);

                // Render the run's lines at its document position
                foreach (var line in lines)
                    line.Y += y;

                node.LineBoxes.AddRange(lines);

                totalHeight += lines.Sum(l => l.Height);
            }

            inlineRun.Clear();
        }

        foreach (var child in node.Children)
        {
            if (child.LayoutType != LayoutType.Block)
            {
                inlineRun.Add(child);
                continue;
            }

            // A block child ends the current inline run
            FinishInlineRun(currentY);

            // Special handling for table rows - layout cells horizontally
            if (child.HtmlNode?.Type == HtmlElementType.TableRow)
            {
                LayoutTableRow(child, innerWidth);
            }
            else
            {
                LayoutBlockNode(child, innerWidth);
            }

            // Position the child element
            child.X = CalculateBlockChildXPosition(node, child, contentWidth);
            child.Y = currentY + child.MarginTop;
            currentY += child.Height + child.MarginTop + child.MarginBottom;
            totalHeight += child.Height + child.MarginTop + child.MarginBottom;
        }

        // Inline run left over at the end of the node
        FinishInlineRun(currentY);

        node.ContentHeight = totalHeight;
        // Height is the border-box: content plus padding, so that borders and the
        // background are drawn around the full box (e.g. table cells).
        node.Height = totalHeight + node.PaddingTop + node.PaddingBottom;
        node.X = node.MarginLeft;
        node.Y = 0;

        MarkLaidOut(node, availableWidth);
    }

    /// <summary>
    /// Recursively collects text content from a node.
    /// </summary>
    private string CollectTextFromNode(LayoutNode node)
    {
        var textBuilder = new System.Text.StringBuilder();

        foreach (var child in node.Children)
        {
            if (child.HtmlNode?.Type == HtmlElementType.Text)
            {
                textBuilder.Append(child.HtmlNode.TextContent ?? string.Empty);
            }
            else if (child.HtmlNode?.Type == HtmlElementType.LineBreak)
            {
                // LineBreak elements insert a newline character to force line break
                textBuilder.Append('\n');
            }
            else
            {
                textBuilder.Append(CollectTextFromNode(child));
            }
        }

        return textBuilder.ToString();
    }

    /// <summary>
    /// Layouts a table row node with cells arranged horizontally.
    /// Cells with an explicit width (CSS width or legacy width attribute) keep that width;
    /// the remaining space is distributed evenly over the flexible cells.
    /// </summary>
    private void LayoutTableRow(LayoutNode rowNode, double availableWidth)
    {
        if (IsLaidOutAt(rowNode, availableWidth))
            return;

        var innerWidth = ResolveInnerWidth(rowNode, availableWidth);
        var cells = rowNode.Children.Where(c => c.LayoutType == LayoutType.Block).ToList();
        var cellCount = cells.Count;
        if (cellCount == 0)
        {
            rowNode.Height = 0;
            rowNode.Width = availableWidth;
            return;
        }

        // Determine each cell's explicit width (if any) and the total flexible colspan.
        var fixedTotal = 0.0;
        var flexibleColspan = 0;
        var cellWidths = new double[cellCount];

        for (var i = 0; i < cellCount; i++)
        {
            if (cells[i].WidthSet)
            {
                cellWidths[i] = Math.Min(cells[i].Width, availableWidth);
                fixedTotal += cellWidths[i];
            }
            else
            {
                flexibleColspan += GetColspan(cells[i]);
            }
        }

        // If the explicit widths exceed the available width, scale them down proportionally
        // so the row never overflows.
        if (fixedTotal > availableWidth && fixedTotal > 0)
        {
            var scale = availableWidth / fixedTotal;
            for (var i = 0; i < cellCount; i++)
            {
                if (cells[i].WidthSet)
                    cellWidths[i] *= scale;
            }
            fixedTotal = availableWidth;
        }

        var flexibleShare = flexibleColspan > 0
            ? Math.Max(availableWidth - fixedTotal, 0) / flexibleColspan
            : 0;

        double currentX = 0;
        double maxCellHeight = 0;

        // Layout each cell horizontally
        for (var i = 0; i < cellCount; i++)
        {
            var cell = cells[i];
            var cellWidth = cell.WidthSet ? cellWidths[i] : flexibleShare * GetColspan(cell);

            // A fixed-width cell may be allocated less space than its explicit width (e.g.
            // when the row's fixed widths exceed the available width and are scaled down);
            // in that case the row allocation wins so the row never overflows.
            if (cell.WidthSet && cellWidth < cell.Width)
                cell.Width = cellWidth;

            // Layout cell with its allocated width
            LayoutBlockNode(cell, cellWidth);

            // Position cell horizontally
            cell.X = currentX;
            cell.Y = 0;

            currentX += cellWidth;
            maxCellHeight = Math.Max(maxCellHeight, cell.Height);
        }

        // Row height is the maximum cell height (including any cell margins)
        rowNode.Width = availableWidth;
        rowNode.ContentWidth = innerWidth;
        rowNode.ContentHeight = maxCellHeight;
        rowNode.Height = maxCellHeight;
        rowNode.X = rowNode.MarginLeft;
        rowNode.Y = 0;

        MarkLaidOut(rowNode, availableWidth);
    }

    /// <summary>
    /// Gets the colspan of a table cell (default 1 when not set or invalid).
    /// </summary>
    private static int GetColspan(LayoutNode cell)
    {
        if (cell.HtmlNode?.Attributes.TryGetValue("colspan", out var value) == true &&
            int.TryParse(value, out var colspan) && colspan > 0)
        {
            return colspan;
        }

        return 1;
    }

    /// <summary>
    /// Parses a legacy HTML width attribute value (e.g. width="640" or width="100%").
    /// Pixel values are clamped to the available width so fixed-width tables never
    /// overflow the viewport; percentage values are resolved against the available width.
    /// </summary>
    private static bool TryParseLegacyWidth(string value, double availableWidth, out double width)
    {
        width = 0;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();

        if (trimmed.EndsWith("%", StringComparison.OrdinalIgnoreCase))
        {
            if (!double.TryParse(trimmed.AsSpan(0, trimmed.Length - 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var percent))
                return false;

            width = availableWidth * (Math.Clamp(percent, 0, 100) / 100.0);
        }
        else if (!double.TryParse(trimmed, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var pixels))
        {
            return false;
        }
        else
        {
            width = Math.Min(Math.Max(pixels, 0), availableWidth);
        }

        return width > 0;
    }

    /// <summary>
    /// Layouts a text node.
    /// </summary>
    private void LayoutTextNode(LayoutNode node, double availableWidth)
    {
        var text = node.HtmlNode?.TextContent ?? string.Empty;

        // Don't trim text - preserve original content for rendering
        // Treat &nbsp; (non-breaking space, U+00A0) as non-empty text
        // Check if text is empty or contains only regular whitespace (not non-breaking spaces)
        // A non-breaking space should be considered as content
        var hasNonEmptyContent = !string.IsNullOrEmpty(text) &&
            (text.Any(c => c == '\u00A0') || text.Any(c => c != ' ' && c != '\t' && c != '\r' && c != '\n'));

        if (IsLaidOutAt(node, availableWidth))
            return;

        if (!hasNonEmptyContent)
        {
            node.Height = 0;
            node.Width = 0;
            MarkLaidOut(node, availableWidth);
            return;
        }

        // Break text into lines
        var lines = BreakTextIntoLines(text, node, availableWidth);
        node.LineBoxes = lines;

        // Calculate total height
        double totalHeight = 0;
        foreach (var line in lines)
        {
            totalHeight += line.Height;
        }

        node.Height = totalHeight;
        node.Width = lines.Count > 0 ? lines.Max(l => l.Width) : 0;

        MarkLaidOut(node, availableWidth);
    }

    private void LayoutInlineNode(LayoutNode node, double availableWidth)
    {
        if (IsLaidOutAt(node, availableWidth))
            return;

        var lines = new List<LineBox>();
        var currentX = 0.0;
        var currentY = 0.0;
        var currentLineWidth = 0.0;
        var currentLineText = new System.Text.StringBuilder();
        var currentLineSpans = new List<(string text, LayoutNode sourceNode)>();
        var charIndex = 0;
        var lineStartChar = 0;
        var innerWidth = (float)(availableWidth - node.PaddingLeft - node.PaddingRight - node.BorderLeftWidth - node.BorderRightWidth);

        // Finalizes the current line (height/baseline derived from the spans on it, see AlignLineSpans)
        // and optionally advances to the next line.
        void FinishCurrentLine(bool advanceY)
        {
            if (currentLineText.Length == 0)
                return;

            var line = CreateLineBox(currentLineText.ToString(), currentLineSpans, currentLineWidth, currentY, lineStartChar, charIndex);
            lines.Add(line);
            if (advanceY)
                currentY += line.Height;

            currentLineText.Clear();
            currentLineSpans.Clear();
            currentLineWidth = 0;
            lineStartChar = charIndex;
        }

        foreach (var child in node.Children)
        {
            if (child.HtmlNode?.Type == HtmlElementType.Text)
            {
                var text = child.HtmlNode.TextContent ?? string.Empty;
                var textWidth = MeasureText(text, child);

                // Check if text fits on current line
                if (currentLineWidth + textWidth <= innerWidth && currentLineWidth > 0)
                {
                    // Fits on current line
                    currentLineText.Append(text);
                    currentLineSpans.Add((text, child));
                    currentLineWidth += textWidth;
                    child.X = currentX + (currentLineWidth - textWidth);
                    child.Y = currentY;
                    charIndex += text.Length;
                }
                else
                {
                    // Start new line if current line has content
                    FinishCurrentLine(advanceY: true);
                    currentX = 0;

                    // Check if text fits on a new line by itself
                    if (textWidth <= innerWidth)
                    {
                        currentLineText.Append(text);
                        currentLineSpans.Add((text, child));
                        currentLineWidth = textWidth;
                        child.X = 0;
                        child.Y = currentY;
                        charIndex += text.Length;
                    }
                    else
                    {
                        // Word too wide - break it
                        var (prefix, suffix) = BreakWord(text, innerWidth, node);
                        
                        if (prefix.Length > 0)
                        {
                            currentLineText.Append(prefix);
                            currentLineSpans.Add((prefix, child));
                            currentLineWidth = MeasureText(prefix, node);
                            child.X = 0;
                            child.Y = currentY;
                            charIndex += prefix.Length;
                            
                            // Add remaining suffix to the next line
                            if (suffix.Length > 0)
                            {
                                // This would require more complex handling for partial text nodes
                                // For now, we'll just add the suffix to the next line
                                FinishCurrentLine(advanceY: true);
                                currentLineText.Append(suffix);
                                currentLineSpans.Add((suffix, child));
                                currentLineWidth = MeasureText(suffix, node);
                            }
                        }
                        else
                        {
                            // Even the first character doesn't fit - force it anyway
                            FinishCurrentLine(advanceY: true);
                            currentLineText.Append(text);
                            currentLineSpans.Add((text, child));
                            currentLineWidth = textWidth;
                            child.X = 0;
                            child.Y = currentY;
                            charIndex += text.Length;
                        }
                    }
                }
            }
            else if (child.HtmlNode?.Type == HtmlElementType.LineBreak)
            {
                // <br/> forces a line break - finalize current line if there's content
                FinishCurrentLine(advanceY: false);

                // Always add an empty line for the <br/> to preserve vertical spacing
                var emptyLine = CreateLineBox(string.Empty, new List<(string text, LayoutNode sourceNode)>(), 0, currentY, charIndex, charIndex, fallbackNode: node);
                lines.Add(emptyLine);
                currentY += emptyLine.Height;

                child.X = 0;
                child.Y = currentY;
            }
            else if (child.LayoutType == LayoutType.Inline)
            {
                // Nested inline element - treat as a unit
                var childWidth = child.Width;
                
                if (currentLineWidth + childWidth <= innerWidth && currentLineWidth > 0)
                {
                    // Fits on current line
                    child.X = currentX + currentLineWidth;
                    child.Y = currentY;
                    currentLineWidth += childWidth;
                }
                else
                {
                    // Start new line
                    FinishCurrentLine(advanceY: true);
                    currentX = 0;
                    child.X = 0;
                    child.Y = currentY;
                }
            }
        }

        // Add final line if there's remaining content
        FinishCurrentLine(advanceY: true);

        // Apply text alignment
        ApplyTextAlignment(lines, node, innerWidth);

        // Calculate character positions for hit testing
        CalculateCharacterPositions(lines, node);

        // Set node dimensions
        node.LineBoxes = lines;
        node.Height = lines.Count > 0 ? lines.Sum(l => l.Height) : 0;
        node.Width = lines.Count > 0 ? lines.Max(l => l.Width) : 0;

        MarkLaidOut(node, availableWidth);
    }

    /// <summary>
    /// Creates a LineBox with style spans tracking which nodes the text comes from.
    /// The line height and per-span baseline offsets are derived from the font metrics of the
    /// spans on the line (see AlignLineSpans).
    /// </summary>
    private LineBox CreateLineBox(string text, List<(string text, LayoutNode sourceNode)> spans, double width, double y, int startCharIndex, int endCharIndex, LayoutNode? fallbackNode = null)
    {
        var line = new LineBox
        {
            Text = text,
            X = 0,
            Y = y,
            Width = width,
            StartCharIndex = startCharIndex,
            EndCharIndex = endCharIndex
        };

        // Build style spans
        var charPos = 0;
        foreach (var (spanText, sourceNode) in spans)
        {
            if (spanText.Length > 0)
            {
                line.StyleSpans.Add(new LineBox.InlineStyleSpan
                {
                    StartIndex = charPos,
                    Length = spanText.Length,
                    SourceNode = sourceNode
                });
                charPos += spanText.Length;
            }
        }

        AlignLineSpans(line, fallbackNode);
        return line;
    }

    /// <summary>
    /// Computes the line height from the tallest span and sets each span's baseline offset so
    /// that all spans on the line are baseline-aligned. For lines with a single font size this
    /// reduces to that size's own baseline.
    /// </summary>
    private void AlignLineSpans(LineBox line, LayoutNode? fallbackNode = null)
    {
        if (line.StyleSpans.Count == 0 && fallbackNode is not null)
        {
            // Empty line (e.g. from <br/>) - use the fallback node's metrics
            var fallback = GetLineMetrics(fallbackNode.FontSize, fallbackNode.FontFamily, fallbackNode.FontBold, fallbackNode.FontItalic);
            line.Height = fallback.LineHeight;
            line.Baseline = fallback.Baseline;
            return;
        }

        var spanMetrics = line.StyleSpans
            .Select(s => GetLineMetrics(s.SourceNode?.FontSize ?? 16, s.SourceNode?.FontFamily, s.SourceNode?.FontBold ?? false, s.SourceNode?.FontItalic ?? false))
            .ToList();

        if (spanMetrics.Count == 0)
            return;

        // The line must fit the largest ascent (top) and the largest descent (bottom) of all
        // spans on it. The line baseline sits at maxAscent + 1 and all spans are baseline
        // aligned on it (browser behavior for mixed font sizes). A span's own baseline is
        // ascent + 1, so its BaselineOffset is the distance from that baseline to the line
        // baseline: a smaller span (ascent < maxAscent) is drawn lower, a span defining the
        // line's tallest ascent keeps an offset of 0.
        var maxAscent = spanMetrics.Max(m => m.Ascent);
        var maxDescent = spanMetrics.Max(m => m.Descent);

        line.Height = maxAscent + maxDescent + 2;
        line.Baseline = maxAscent + 1;

        for (var i = 0; i < line.StyleSpans.Count; i++)
        {
            var (ascent, _, _, _) = spanMetrics[i];
            line.StyleSpans[i].BaselineOffset = Math.Max(0, maxAscent - ascent);
        }
    }

    /// <summary>
    /// Gets or creates a typeface for the given font style, caching it to avoid repeated lookups.
    /// </summary>
    private SKTypeface GetOrCreateTypeface(string family, SKFontStyleWeight weight, SKFontStyleSlant slant)
    {
        var key = new FontStyleKey(family, weight, slant);
        if (!_typefaceCache.TryGetValue(key, out var typeface))
        {
            typeface = SKTypeface.FromFamilyName(family, weight, SKFontStyleWidth.Normal, slant);
            _typefaceCache[key] = typeface;
        }
        return typeface;
    }

    /// <summary>
    /// Measures the width of text using the specified font.
    /// </summary>
    private double MeasureText(string text, LayoutNode node)
    {
        var typeface = GetOrCreateTypeface(node.FontFamily ?? _defaultFontFamily,
            node.FontBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            node.FontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
        using var font = new SKFont(typeface) { Size = (float)node.FontSize };
        return font.MeasureText(text);
    }

    private double MeasureText(ReadOnlySpan<char> text, LayoutNode node)
    {
        var typeface = GetOrCreateTypeface(
            node.FontFamily ?? _defaultFontFamily,
            node.FontBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            node.FontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);

        using var font = new SKFont(typeface)
        {
            Size = (float)node.FontSize
        };

        return font.MeasureText(text);
    }

    /// <summary>
    /// Breaks a word into two parts that fit within the available width.
    /// </summary>
    private (string prefix, string suffix) BreakWord(
        string word,
        double availableWidth,
        LayoutNode node)
    {
        var span = word.AsSpan();

        // Check if even the first character fits.
        if (MeasureText(span[..1], node) > availableWidth)
            return (string.Empty, word);

        // Binary search for the longest prefix that fits.
        int left = 1;
        int right = span.Length;
        int bestFit = 1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;

            if (MeasureText(span[..mid], node) <= availableWidth)
            {
                bestFit = mid;
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }

        return (
            span[..bestFit].ToString(),
            span[bestFit..].ToString()
        );
    }

    /// <summary>
    /// Calculates the X position for a block child based on parent's text alignment.
    /// </summary>
    private double CalculateBlockChildXPosition(LayoutNode parent, LayoutNode child, double contentWidth)
    {
        // If parent has text-align: center, center the block child
        if (parent.TextAlign == Imapster.HtmlViewer.Parsing.TextAlignment.Center && child.Width < contentWidth)
        {
            return (contentWidth - child.Width) / 2;
        }
        
        // Block elements typically start at X=0 relative to their containing block
        // Margins are handled separately
        return 0;
    }

    /// <summary>
    /// Collects all inline content (text and inline elements) in order with their character positions.
    /// </summary>
    private InlineContent CollectInlineContent(List<LayoutNode> inlineChildren)
    {
        var content = new InlineContent();
        var textBuilder = new System.Text.StringBuilder();
        int charIndex = 0;

        foreach (var child in inlineChildren)
        {
            if (child.HtmlNode?.Type == HtmlElementType.Text)
            {
                var text = child.HtmlNode.TextContent ?? string.Empty;
                textBuilder.Append(text);
                charIndex += text.Length;
            }
            else if (child.HtmlNode?.Type == HtmlElementType.LineBreak)
            {
                // LineBreak elements insert a newline character to force line break
                textBuilder.Append('\n');
                charIndex += 1;
            }
            else if (child.LayoutType == LayoutType.Inline)
            {
                // Collect text from inline element
                var text = CollectTextFromNode(child);
                textBuilder.Append(text);
                charIndex += text.Length;
            }
        }

        content.CombinedText = textBuilder.ToString();
        content.HasContent = textBuilder.Length > 0;
        return content;
    }

    /// <summary>
    /// Maps the characters of a single inline child (in order) to their top-level source node.
    /// Nested inline elements are attributed to the top-level inline child so that a line gets
    /// one style span per top-level styled element (matching how the inline flow is laid out).
    /// </summary>
    private void MapInlineChildSource(LayoutNode child, LayoutNode topLevelSource, List<LayoutNode> map)
    {
        var source = child.LayoutType == LayoutType.Inline ? topLevelSource : child;

        if (child.HtmlNode?.Type == HtmlElementType.Text)
        {
            var text = child.HtmlNode.TextContent ?? string.Empty;
            for (var i = 0; i < text.Length; i++)
                map.Add(source);
        }
        else if (child.HtmlNode?.Type == HtmlElementType.LineBreak)
        {
            map.Add(source);
        }
        else
        {
            foreach (var grandChild in child.Children)
                MapInlineChildSource(grandChild, topLevelSource, map);
        }
    }

    /// <summary>
    /// Builds a map of absolute character index -> source layout node for the combined inline
    /// text (same order as CollectInlineContent). When no inline children are provided, every
    /// character maps to the node itself.
    /// </summary>
    private List<LayoutNode> BuildCharSourceMap(string text, LayoutNode node, List<LayoutNode>? inlineChildren)
    {
        var map = new List<LayoutNode>(text.Length);

        if (inlineChildren is not null)
        {
            foreach (var child in inlineChildren)
            {
                MapInlineChildSource(child, child, map);
            }
        }
        else
        {
            for (var i = 0; i < text.Length; i++)
                map.Add(node);
        }

        return map;
    }

    /// <summary>
    /// Builds the style spans of a line (absolute character range -> relative spans pointing at
    /// the source node) and derives the line height/baseline from those spans.
    /// </summary>
    private void BuildLineStyleSpans(LineBox line, List<LayoutNode> charSources)
    {
        if (string.IsNullOrEmpty(line.Text))
            return;

        var absStart = Math.Max(0, line.StartCharIndex);
        // The absolute range may include a trailing space that is not part of the line text
        var absEnd = Math.Min(charSources.Count, absStart + line.Text.Length);

        LayoutNode? source = null;
        var spanStart = absStart;

        for (var i = absStart; i < absEnd; i++)
        {
            var current = i < charSources.Count ? charSources[i] : null;

            if (source is not null && ReferenceEquals(current, source))
                continue;

            if (source is not null)
            {
                line.StyleSpans.Add(new LineBox.InlineStyleSpan
                {
                    StartIndex = spanStart - absStart,
                    Length = i - spanStart,
                    SourceNode = source
                });
            }

            source = current;
            spanStart = i;
        }

        if (source is not null)
        {
            line.StyleSpans.Add(new LineBox.InlineStyleSpan
            {
                StartIndex = spanStart - absStart,
                Length = absEnd - spanStart,
                SourceNode = source
            });
        }

        AlignLineSpans(line);
    }

    /// <summary>
    /// Breaks text into lines based on available width.
    /// Line height/baseline are derived from font metrics of the spans on each line, so mixed
    /// font-size lines get a correct (larger) line height.
    /// The running line width is carried forward as a cumulative estimate (word and space
    /// widths) instead of re-measuring the whole line per word, and each finalized line is
    /// measured exactly once - so the cost grows linearly with the text length rather than
    /// with the square of the line length.
    /// </summary>
    private List<LineBox> BreakTextIntoLines(string text, LayoutNode node, double availableWidth, List<LayoutNode>? inlineChildren = null)
    {
        var lines = new List<LineBox>();
        var charSources = BuildCharSourceMap(text, node, inlineChildren);
        var currentY = 0.0;
        var currentLineText = new System.Text.StringBuilder();
        var currentWidth = 0.0;
        var charIndex = 0;
        var lineStartChar = 0;
        // The space width is constant for the node's font, so measure it once instead of per word.
        var spaceWidth = MeasureText(" ", node);

        // Emits the accumulated line, measuring its full text exactly once for the LineBox width.
        void EmitCurrentLine()
        {
            if (currentLineText.Length == 0)
                return;

            var lineText = currentLineText.ToString();
            var line = new LineBox
            {
                Text = lineText,
                X = 0,
                Y = currentY,
                Width = MeasureText(lineText, node),
                StartCharIndex = lineStartChar,
                EndCharIndex = charIndex
            };
            BuildLineStyleSpans(line, charSources);
            lines.Add(line);

            currentY += line.Height;
            lineStartChar = charIndex;
            currentLineText.Clear();
            currentWidth = 0;
        }

        // Handle newlines explicitly (from <br/> elements)
        var segments = text.Split('\n');
        var segmentIndex = 0;

        while (segmentIndex < segments.Length)
        {
            var segment = segments[segmentIndex];
            var words = segment.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in words)
            {
                var wordWidth = MeasureText(word, node);
                var totalWidth = currentWidth + wordWidth + (currentLineText.Length > 0 ? spaceWidth : 0);

                if (totalWidth <= availableWidth && currentLineText.Length > 0)
                {
                    // Word fits on current line - carry the running estimate forward; the
                    // line is measured once when it is emitted.
                    currentLineText.Append(" ").Append(word);
                    currentWidth = totalWidth;
                }
                else
                {
                    EmitCurrentLine();

                    // Start new line with current word
                    currentLineText.Append(word);
                    currentWidth = wordWidth;
                }

                charIndex += word.Length + 1; // +1 for space
            }

            // Handle end of segment (either end of text or newline)
            var hadContent = currentLineText.Length > 0;
            EmitCurrentLine();
            if (!hadContent && segmentIndex < segments.Length - 1)
            {
                // Empty segment (consecutive <br/>) - add empty line to reserve vertical space
                var emptyLine = new LineBox
                {
                    Text = string.Empty,
                    X = 0,
                    Y = currentY,
                    Width = 0,
                    StartCharIndex = charIndex,
                    EndCharIndex = charIndex
                };
                AlignLineSpans(emptyLine, node);
                lines.Add(emptyLine);
                currentY += emptyLine.Height;
            }

            charIndex++; // Account for newline character
            segmentIndex++;
        }

        return lines;
    }

    /// <summary>
    /// Layouts inline children across the broken lines, distributing them based on text position.
    /// Each child's text, width and LineBox are computed once; only the per-line placement
    /// (X/Y and the line height) varies, so a child spanning N lines costs one full-text
    /// measurement instead of N.
    /// </summary>
    private void LayoutInlineChildrenOnLines(List<LayoutNode> inlineChildren, List<LineBox> lines, LayoutNode parentNode)
    {
        if (lines.Count == 0)
            return;

        // Build a map of character ranges for each child, with the child's text and width
        // precomputed once (the old code re-measured each child's full text per line it
        // spanned, which made layout quadratic for a single long text run).
        var childRanges = new List<(int start, int end, LayoutNode child, string text, double textWidth, LineBox? lineBox)>();
        int charIndex = 0;

        foreach (var child in inlineChildren)
        {
            if (child.HtmlNode?.Type == HtmlElementType.Text)
            {
                var text = child.HtmlNode.TextContent ?? string.Empty;
                // Text children are rendered through the parent's line boxes (their layout
                // type is None, so the renderer never reads their LineBoxes); only their
                // text and width are needed for placement.
                childRanges.Add((charIndex, charIndex + text.Length, child, text, MeasureText(text, parentNode), null));
                charIndex += text.Length;
            }
            else if (child.HtmlNode?.Type == HtmlElementType.LineBreak)
            {
                charIndex += 1;
            }
            else if (child.LayoutType == LayoutType.Inline)
            {
                var text = CollectTextFromNode(child);
                var textWidth = MeasureText(text, child);

                // One LineBox per child, not one per overlapping line: the renderer only
                // matches a child LineBox against a parent line when the child's full text
                // is the entire line, so copies for the other lines of a multi-line child
                // could never be found.
                var lineBox = new LineBox
                {
                    Text = text,
                    X = 0,
                    Y = 0,
                    Width = textWidth
                };

                // Style span pointing to the child (e.g. link) node
                lineBox.StyleSpans.Add(new LineBox.InlineStyleSpan
                {
                    StartIndex = 0,
                    Length = text.Length,
                    SourceNode = child
                });
                AlignLineSpans(lineBox);

                childRanges.Add((charIndex, charIndex + text.Length, child, text, textWidth, lineBox));
                charIndex += text.Length;
            }
        }

        // Layout each line and position inline children that belong on that line
        for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            var line = lines[lineIndex];
            var lineStartY = line.Y;

            var lineStartChar = line.StartCharIndex >= 0 ? line.StartCharIndex : 0;
            var lineEndChar = line.EndCharIndex >= 0 ? line.EndCharIndex : (lineStartChar + (line.Text?.Length ?? 0));

            double currentX = 0;

            foreach (var (rangeStart, rangeEnd, child, _, textWidth, lineBox) in childRanges)
            {
                if (rangeEnd <= lineStartChar || rangeStart >= lineEndChar)
                    continue;

                child.X = currentX;
                child.Y = lineStartY;
                child.Width = textWidth;
                child.Height = line.Height;

                if (lineBox is not null)
                    child.LineBoxes.Add(lineBox);

                currentX += textWidth;
            }
        }
    }

    /// <summary>
    /// Helper class to hold inline content data.
    /// </summary>
    private class InlineContent
    {
        public string CombinedText { get; set; } = string.Empty;
        public bool HasContent { get; set; }
    }

    /// <summary>
    /// Applies text alignment to line boxes.
    /// </summary>
    private void ApplyTextAlignment(List<LineBox> lines, LayoutNode node, double innerWidth)
    {
        foreach (var line in lines)
        {
            switch (node.TextAlign)
            {
                case Parsing.TextAlignment.Center:
                    line.X = (innerWidth - line.Width) / 2;
                    break;
                case Parsing.TextAlignment.Right:
                    line.X = innerWidth - line.Width;
                    break;
                case Parsing.TextAlignment.Justify:
                    // Justification would require more complex logic to adjust word spacing
                    // For now, we'll just left-align
                    line.X = 0;
                    break;
                default:
                    line.X = 0;
                    break;
            }
        }
    }

    /// <summary>
    /// Calculates character positions for hit testing.
    /// </summary>
    private void CalculateCharacterPositions(List<LineBox> lines, LayoutNode node)
    {
        // This method would calculate the position of each character in the line boxes
        // for hit testing purposes. For now, we use the StartCharIndex and EndCharIndex
        // that are already set on the LineBoxes.
    }
}

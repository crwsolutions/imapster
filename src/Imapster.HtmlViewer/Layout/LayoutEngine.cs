using Imapster.HtmlViewer.Parsing;
using SkiaSharp;

namespace Imapster.HtmlViewer.Layout;

/// <summary>
/// Engine for calculating layout of parsed HTML content.
/// </summary>
public sealed class LayoutEngine
{
    private readonly SKTypeface _defaultTypeface;
    private readonly double _defaultFontSize;
    private readonly string _defaultFontFamily;

    // Default margins for block elements (in em units, relative to font size)
    private const double DefaultParagraphMargin = 1.0;
    private const double DefaultHeadingMarginTop = 0.67;
    private const double DefaultHeadingMarginBottom = 0.67;
    private const double DefaultBlockquoteMargin = 1.0;
    private const double DefaultListMargin = 1.0;
    private const double DefaultListItemPadding = 0.5;
    private const double DefaultMarkerWidth = 2.5; // Width for list markers in em (bullet + space)

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
            BorderBottomColor = mergedStyle.BorderBottomColor
        };

        // Apply default margins for block elements if not explicitly set
        ApplyDefaultMargins(layoutNode, htmlNode.Type, mergedStyle.FontSize);

        // Process children
        foreach (var child in htmlNode.Children)
        {
            var childLayoutNode = ConvertHtmlNode(child, mergedStyle, availableWidth);
            childLayoutNode.Parent = layoutNode;
            layoutNode.Children.Add(childLayoutNode);
        }

        // Layout the node
        if (layoutNode.LayoutType == LayoutType.Block)
        {
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
        var contentWidth = node.WidthSet ? node.Width : availableWidth - node.MarginLeft - node.MarginRight;

        // Calculate content width accounting for padding and borders
        var innerWidth = contentWidth - node.PaddingLeft - node.PaddingRight - node.BorderLeftWidth - node.BorderRightWidth;

        node.Width = contentWidth;
        node.ContentWidth = innerWidth;

        // Separate block children from inline children
        var blockChildren = new List<LayoutNode>();
        var inlineChildren = new List<LayoutNode>();

        foreach (var child in node.Children)
        {
            if (child.LayoutType == LayoutType.Block)
            {
                blockChildren.Add(child);
            }
            else
            {
                inlineChildren.Add(child);
            }
        }

        // Layout block children first
        double totalHeight = 0;
        double currentY = 0;

        foreach (var child in blockChildren)
        {
            // Special handling for table rows - layout cells horizontally
            if (child.HtmlNode?.Type == HtmlElementType.TableRow)
            {
                LayoutTableRow(child, innerWidth);
            }
            else
            {
                LayoutBlockNode(child, innerWidth);
            }

            child.X = node.PaddingLeft + node.BorderLeftWidth;
            child.Y = currentY + child.MarginTop;
            currentY += child.Height + child.MarginTop + child.MarginBottom;
            totalHeight += child.Height + child.MarginTop + child.MarginBottom;
        }

        // Handle inline children - they flow horizontally within lines
        if (inlineChildren.Count > 0)
        {
            // Collect all inline content (text and inline elements) to create unified line breaking
            var inlineContent = CollectInlineContent(inlineChildren);

            if (!inlineContent.HasContent)
            {
                // No text content in inline children
                currentY += 0;
                totalHeight += 0;
            }
            else
            {
                // Create line boxes that wrap all inline content together
                var lines = BreakTextIntoLines(inlineContent.CombinedText, node, innerWidth);
                node.LineBoxes = lines;

                // Distribute inline elements across lines based on text positions
                LayoutInlineChildrenOnLines(inlineChildren, lines, inlineContent, node);

                var inlineHeight = lines.Count > 0 ? lines.Sum(l => l.Height) : 0;
                currentY += inlineHeight;
                totalHeight += inlineHeight;
            }
        }

        node.Height = totalHeight;
        node.X = node.MarginLeft;
        node.Y = 0;
    }

    /// <summary>
    /// Collects text content from inline children.
     /// </summary>
    private string CollectInlineTextContent(List<LayoutNode> inlineChildren)
    {
        var textBuilder = new System.Text.StringBuilder();

        foreach (var child in inlineChildren)
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
            else if (child.LayoutType == LayoutType.Inline)
            {
                // Recursively collect text from inline element children
                textBuilder.Append(CollectTextFromNode(child));
            }
        }

        return textBuilder.ToString();
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
    /// </summary>
    private void LayoutTableRow(LayoutNode rowNode, double availableWidth)
    {
        var cellCount = rowNode.Children.Count;
        if (cellCount == 0)
        {
            rowNode.Height = 0;
            rowNode.Width = availableWidth;
            return;
        }

        // Calculate width for each cell
        var cellWidth = availableWidth / cellCount;

        double currentX = 0;
        double maxCellHeight = 0;

        // Layout each cell horizontally
        foreach (var cell in rowNode.Children)
        {
            if (cell.LayoutType == LayoutType.Block)
            {
                // Layout cell with its allocated width
                LayoutBlockNode(cell, cellWidth);

                // Position cell horizontally
                cell.X = currentX;
                cell.Y = 0;

                currentX += cellWidth;
                maxCellHeight = Math.Max(maxCellHeight, cell.Height);
            }
        }

        // Row height is the maximum cell height (including any cell margins)
        rowNode.Width = availableWidth;
        rowNode.Height = maxCellHeight;
    }

    /// <summary>
    /// Layouts a list item node.
    /// </summary>
    private void LayoutListItemNode(LayoutNode node, double availableWidth)
    {
        // Layout children
        double totalHeight = 0;
        double currentY = 0;

        foreach (var child in node.Children)
        {
            if (child.LayoutType == LayoutType.Block)
            {
                LayoutBlockNode(child, availableWidth);
                child.X = 0;
                child.Y = currentY;
                currentY += child.Height + child.MarginTop + child.MarginBottom;
                totalHeight += child.Height + child.MarginTop + child.MarginBottom;
            }
            else if (child.LayoutType == LayoutType.Inline)
            {
                LayoutInlineNode(child, availableWidth);
                child.X = 0;
                child.Y = currentY;
                currentY += child.Height + child.MarginTop + child.MarginBottom;
                totalHeight += child.Height + child.MarginTop + child.MarginBottom;
            }
            else if (child.LayoutType == LayoutType.None && child.HtmlNode?.Type == HtmlElementType.Text)
            {
                LayoutTextNode(child, availableWidth);
                child.X = 0;
                child.Y = currentY;
                currentY += child.Height + child.MarginTop + child.MarginBottom;
                totalHeight += child.Height + child.MarginTop + child.MarginBottom;
            }
        }

        node.Height = totalHeight;
        node.X = 0;
        node.Y = 0;
    }

    /// <summary>
    /// Layouts a text node.
    /// </summary>
    private void LayoutTextNode(LayoutNode node, double availableWidth)
    {
        var text = node.HtmlNode?.TextContent ?? string.Empty;
        
        // Don't trim text - preserve original content for rendering
        if (string.IsNullOrEmpty(text))
        {
            node.Height = 0;
            node.Width = 0;
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
    }

    /// <summary>
    /// Layouts an inline-level node.
    /// </summary>
    private void LayoutInlineNode(LayoutNode node, double availableWidth)
    {
        var contentWidth = node.WidthSet ? node.Width : availableWidth - node.MarginLeft - node.MarginRight;
        var innerWidth = contentWidth - node.PaddingLeft - node.PaddingRight - node.BorderLeftWidth - node.BorderRightWidth;

        node.ContentWidth = innerWidth;

        // Check if this inline node has only text children (no nested inline elements)
        var hasOnlyTextChildren = node.Children.All(c => c.HtmlNode?.Type == HtmlElementType.Text);
        
        if (hasOnlyTextChildren)
        {
            // Collect all text content from direct text children
            var fullText = string.Join("", node.Children.Where(c => c.HtmlNode?.Type == HtmlElementType.Text)
                .Select(c => c.HtmlNode?.TextContent ?? string.Empty));

            // Measure and break into lines
            var lines = BreakTextIntoLines(fullText, node, innerWidth);
            node.LineBoxes = lines;
            
            // Calculate dimensions from line boxes
            node.Height = lines.Count > 0 ? lines.Sum(l => l.Height) : 0;
            node.Width = lines.Count > 0 ? lines.Max(l => l.Width) : 0;
            
            // Set X positions for text children so they render in sequence
            double currentX = 0;
            foreach (var child in node.Children)
            {
                if (child.HtmlNode?.Type == HtmlElementType.Text)
                {
                    var text = child.HtmlNode.TextContent ?? string.Empty;
                    child.X = currentX;
                    child.Y = 0;
                    currentX += MeasureText(text, node);
                    child.Width = currentX - child.X;
                    child.Height = node.Height;
                }
            }
        }
        else
        {
            // Has nested inline elements - collect all text and layout as a single flowing unit
            var textParts = new List<(string text, LayoutNode node)>();

            // First pass: collect all text content in order with their style nodes
            CollectInlineTextWithNodes(node, textParts);

            if (textParts.Count == 0)
            {
                // No text content
                node.LineBoxes.Clear();
                node.Height = 0;
                node.Width = 0;
            }
            else
            {
                // Combine all text and create a single line-breaking pass
                var fullText = string.Concat(textParts.Select(p => p.text));
                var lineHeight = node.FontSize * 1.2;
                var lines = BreakTextIntoLines(fullText, node, innerWidth);

                // Now distribute children across lines based on text positions
                var charIndex = 0;
                var childIndex = 0;

                foreach (var line in lines)
                {
                    var lineEndChar = charIndex + (line.Text?.Length ?? 0);

                    // Find which children fall into this line
                    while (childIndex < textParts.Count && charIndex < lineEndChar)
                    {
                        var (childText, childNode) = textParts[childIndex];
                        var childEndChar = charIndex + childText.Length;

                        if (childEndChar <= lineEndChar)
                        {
                            // Child is entirely within this line
                            childNode.Y = line.Y;
                            childIndex++;
                            charIndex = childEndChar;
                        }
                        else
                        {
                            // Child spans multiple lines - just set its position
                            childNode.Y = line.Y;
                            charIndex = lineEndChar;
                            break;
                        }
                    }
                }

                // Layout children to get their widths
                double currentX = 0;
                foreach (var child in node.Children)
                {
                    if (child.LayoutType == LayoutType.Inline)
                    {
                        LayoutInlineNode(child, innerWidth);
                    }
                    else if (child.HtmlNode?.Type == HtmlElementType.Text)
                    {
                        LayoutTextNode(child, innerWidth);
                    }

                    child.X = currentX;
                    currentX += child.Width;
                }

                // Set paragraph dimensions from lines
                node.LineBoxes = lines;
                node.Height = lines.Count > 0 ? lines.Sum(l => l.Height) : 0;
                node.Width = lines.Count > 0 ? lines.Max(l => l.Width) : 0;
            }
        }
    }

    /// <summary>
    /// Collects text content and their associated style nodes from inline children.
    /// </summary>
    private void CollectInlineTextWithNodes(LayoutNode node, List<(string text, LayoutNode node)> textParts)
    {
        foreach (var child in node.Children)
        {
            if (child.HtmlNode?.Type == HtmlElementType.Text)
            {
                textParts.Add((child.HtmlNode.TextContent ?? string.Empty, child));
            }
            else if (child.LayoutType == LayoutType.Inline)
            {
                CollectInlineTextWithNodes(child, textParts);
            }
        }
    }

    /// <summary>
    /// Collects text content from inline nodes.
    /// </summary>
    private void CollectInlineText(LayoutNode node, List<string> textParts, List<HtmlStyle> styles)
    {
        foreach (var child in node.Children)
        {
            if (child.LayoutType == LayoutType.Inline)
            {
                CollectInlineText(child, textParts, styles);
            }
            else if (child.HtmlNode?.Type == HtmlElementType.Text)
            {
                textParts.Add(child.HtmlNode.TextContent ?? string.Empty);
                styles.Add(child.HtmlNode.Style ?? new HtmlStyle());
            }
        }
    }

    /// <summary>
    /// Breaks text into lines based on available width.
    /// </summary>
    private List<LineBox> BreakTextIntoLines(string text, LayoutNode node, double availableWidth)
    {
        var lines = new List<LineBox>();
        var lineHeight = node.FontSize * 1.2;

        // Check if this is a preformatted block
        var isPreformatted = node.HtmlNode?.Type == HtmlElementType.Pre || 
                             (node.Parent?.HtmlNode?.Type == HtmlElementType.Pre == true);

        if (isPreformatted)
        {
            return BreakPreformattedText(text, node, availableWidth);
        }

        // Check if text contains explicit newlines
        if (text.Contains('\n'))
        {
            return BreakTextWithNewlines(text, node, availableWidth);
        }

        // Ensure minimum available width to prevent infinite loops
        var effectiveAvailableWidth = Math.Max(availableWidth, node.FontSize);

        var words = SplitIntoWords(text);
        var currentLine = new LineBox();
        var currentWidth = 0.0;
        var charIndex = 0;
        var currentY = 0.0;

        foreach (var word in words)
        {
            var wordWidth = MeasureText(word, node);
            var spaceWidth = currentLine.Text.Length > 0 ? MeasureText(" ", node) : 0;

            if (currentWidth + wordWidth + spaceWidth <= effectiveAvailableWidth || currentLine.Text.Length == 0)
            {
                // Add word to current line
                if (currentLine.Text.Length > 0)
                {
                    currentLine.Text += " ";
                    currentWidth += spaceWidth;
                }
                currentLine.Text += word;
                currentWidth += wordWidth;
                charIndex += word.Length + (currentLine.Text.Length > word.Length ? 1 : 0);
            }
            else
            {
                // Finalize current line
                if (currentLine.Text.Length > 0)
                {
                    currentLine.StartCharIndex = charIndex - currentLine.Text.Length;
                    currentLine.EndCharIndex = charIndex;
                    currentLine.Width = currentWidth;
                    currentLine.Height = lineHeight;
                    currentLine.Baseline = node.FontSize;
                    currentLine.Y = currentY;
                    lines.Add(currentLine);

                    currentLine = new LineBox();
                    currentWidth = 0;
                    currentY += lineHeight;
                }

                // Check if word itself is too wide
                if (wordWidth > effectiveAvailableWidth)
                {
                    // Break word
                    var (prefix, suffix) = BreakWord(word, effectiveAvailableWidth, node);
                    if (prefix.Length > 0)
                    {
                        currentLine.Text = prefix;
                        currentLine.StartCharIndex = charIndex;
                        currentLine.EndCharIndex = charIndex + prefix.Length;
                        currentLine.Width = MeasureText(prefix, node);
                        currentLine.Height = lineHeight;
                        currentLine.Baseline = node.FontSize;
                        currentLine.Y = currentY;
                        lines.Add(currentLine);
                        currentLine = new LineBox();
                        currentWidth = 0;
                        currentY += lineHeight;
                        charIndex += prefix.Length;
                    }

                    if (suffix.Length > 0)
                    {
                        currentLine.Text = suffix;
                        currentLine.StartCharIndex = charIndex;
                        currentLine.EndCharIndex = charIndex + suffix.Length;
                        currentWidth = MeasureText(suffix, node);
                        charIndex += suffix.Length;
                    }
                }
                else
                {
                    // Start new line with word
                    currentLine.Text = word;
                    currentLine.StartCharIndex = charIndex;
                    currentWidth = wordWidth;
                    charIndex += word.Length;
                }
            }
        }

        // Add remaining text as last line
        if (currentLine.Text.Length > 0)
        {
            currentLine.StartCharIndex = charIndex - currentLine.Text.Length;
            currentLine.EndCharIndex = charIndex;
            currentLine.Width = currentWidth;
            currentLine.Height = lineHeight;
            currentLine.Baseline = node.FontSize;
            currentLine.Y = currentY;
            lines.Add(currentLine);
        }

        ApplyTextAlignment(lines, node, effectiveAvailableWidth);
        CalculateCharacterPositions(lines, node);

        return lines;
    }

    /// <summary>
    /// Breaks text with explicit newlines into lines, respecting both newlines and word wrapping.
    /// </summary>
    private List<LineBox> BreakTextWithNewlines(string text, LayoutNode node, double availableWidth)
    {
        var lines = new List<LineBox>();
        var lineHeight = node.FontSize * 1.2;
        var currentY = 0.0;
        var charIndex = 0;

        // Split by newlines but preserve content
        var lineTexts = text.Split('\n');

        foreach (var lineText in lineTexts)
        {
            if (string.IsNullOrEmpty(lineText))
            {
                // Empty line - just create an empty line box
                var emptyLine = new LineBox
                {
                    Text = "",
                    Width = 0,
                    Height = lineHeight,
                    Baseline = node.FontSize,
                    Y = currentY,
                    StartCharIndex = charIndex,
                    EndCharIndex = charIndex
                };
                lines.Add(emptyLine);
                currentY += lineHeight;
                charIndex += 1; // Account for the newline character
                continue;
            }

            // For each line, apply word wrapping
            var words = SplitIntoWords(lineText);
            var currentLine = new LineBox();
            var currentWidth = 0.0;
            var lineStartChar = charIndex;

            foreach (var word in words)
            {
                var wordWidth = MeasureText(word, node);
                var spaceWidth = currentLine.Text.Length > 0 ? MeasureText(" ", node) : 0;

                if (currentWidth + wordWidth + spaceWidth <= availableWidth)
                {
                    // Add word to current line
                    if (currentLine.Text.Length > 0)
                    {
                        currentLine.Text += " ";
                        currentWidth += spaceWidth;
                    }
                    currentLine.Text += word;
                    currentWidth += wordWidth;
                }
                else
                {
                    // Finalize current line
                    if (currentLine.Text.Length > 0)
                    {
                        currentLine.EndCharIndex = charIndex;
                        currentLine.Width = currentWidth;
                        currentLine.Height = lineHeight;
                        currentLine.Baseline = node.FontSize;
                        currentLine.Y = currentY;
                        currentLine.StartCharIndex = lineStartChar;
                        lines.Add(currentLine);

                        // Start new line
                        currentLine = new LineBox();
                        currentWidth = 0;
                        currentY += lineHeight;
                        lineStartChar = charIndex;
                    }

                    // Check if word itself is too wide
                    if (wordWidth > availableWidth)
                    {
                        // Break word
                        var (prefix, suffix) = BreakWord(word, availableWidth, node);
                        if (prefix.Length > 0)
                        {
                            currentLine.Text = prefix;
                            currentLine.EndCharIndex = charIndex + prefix.Length;
                            currentLine.Width = MeasureText(prefix, node);
                            currentLine.Height = lineHeight;
                            currentLine.Baseline = node.FontSize;
                            currentLine.Y = currentY;
                            currentLine.StartCharIndex = lineStartChar;
                            lines.Add(currentLine);
                            currentLine = new LineBox();
                            currentWidth = 0;
                            currentY += lineHeight;
                            lineStartChar = charIndex + prefix.Length;
                            charIndex += prefix.Length;
                        }

                        if (suffix.Length > 0)
                        {
                            currentLine.Text = suffix;
                            currentLine.EndCharIndex = charIndex + suffix.Length;
                            currentWidth = MeasureText(suffix, node);
                            charIndex += suffix.Length;
                        }
                    }
                    else
                    {
                        // Start new line with word
                        currentLine.Text = word;
                        currentWidth = wordWidth;
                    }
                }
                charIndex += word.Length + 1; // +1 for space
            }

            // Add remaining text on this line
            if (currentLine.Text.Length > 0)
            {
                currentLine.EndCharIndex = charIndex - 1; // -1 to exclude the space after last word
                currentLine.Width = currentWidth;
                currentLine.Height = lineHeight;
                currentLine.Baseline = node.FontSize;
                currentLine.Y = currentY;
                currentLine.StartCharIndex = lineStartChar;
                lines.Add(currentLine);
            }

            currentY += lineHeight;
            charIndex += 1; // Account for the newline character
        }

        // Apply text alignment
        ApplyTextAlignment(lines, node, availableWidth);

        // Calculate character positions for hit testing
        CalculateCharacterPositions(lines, node);

        return lines;
    }

    /// <summary>
    /// Breaks preformatted text into lines, preserving newlines and whitespace.
    /// </summary>
    private List<LineBox> BreakPreformattedText(string text, LayoutNode node, double availableWidth)
    {
        var lines = new List<LineBox>();
        var lineHeight = node.FontSize * 1.2;
        var currentY = 0.0;
        var charIndex = 0;

        // Split by newlines but preserve the content
        var lineTexts = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var lineText in lineTexts)
        {
            if (string.IsNullOrEmpty(lineText))
                continue;

            var line = new LineBox();
            line.Text = lineText;
            line.Width = MeasureText(lineText, node);
            line.Height = lineHeight;
            line.Baseline = node.FontSize;
            line.Y = currentY;
            line.StartCharIndex = charIndex;
            line.EndCharIndex = charIndex + lineText.Length;
            
            lines.Add(line);
            
            currentY += lineHeight;
            charIndex += lineText.Length + 1; // +1 for the newline character
        }

        // Calculate character positions
        CalculateCharacterPositions(lines, node);

        return lines;
    }

    /// <summary>
    /// Splits text into words.
    /// </summary>
    private List<string> SplitIntoWords(string text)
    {
        var words = new List<string>();
        var currentWord = new System.Text.StringBuilder();

        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (currentWord.Length > 0)
                {
                    words.Add(currentWord.ToString());
                    currentWord.Clear();
                }
            }
            else
            {
                currentWord.Append(c);
            }
        }

        if (currentWord.Length > 0)
        {
            words.Add(currentWord.ToString());
        }

        return words;
    }

    /// <summary>
    /// Breaks a word that is too wide for the available space.
    /// </summary>
    private (string WordPrefix, string WordSuffix) BreakWord(string word, double maxWidth, LayoutNode node)
    {
        var left = 0;
        var right = word.Length;

        while (left < right)
        {
            var mid = (left + right) / 2;
            var testPrefix = word.Substring(0, mid);
            var prefixWidth = MeasureText(testPrefix, node);

            if (prefixWidth <= maxWidth)
            {
                left = mid + 1;
            }
            else
            {
                right = mid;
            }
        }

        var wordPrefix = word.Substring(0, left);
        var wordSuffix = word.Substring(left);

        return (wordPrefix, wordSuffix);
    }

    /// <summary>
    /// Applies text alignment to lines.
    /// </summary>
    private void ApplyTextAlignment(List<LineBox> lines, LayoutNode node, double availableWidth)
    {
        foreach (var line in lines)
        {
            var remainingSpace = availableWidth - line.Width;

            switch (node.TextAlign)
            {
                case Parsing.TextAlignment.Right:
                    line.X = remainingSpace;
                    break;

                case Parsing.TextAlignment.Center:
                    line.X = remainingSpace / 2;
                    break;

                case Parsing.TextAlignment.Justify:
                    if (lines.Count > 1 && lines.IndexOf(line) < lines.Count - 1)
                    {
                        // Justify: distribute space between words
                        var wordCount = line.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                        if (wordCount > 1)
                        {
                            var spaceWidth = MeasureText(" ", node);
                            var extraSpace = remainingSpace / (wordCount - 1);
                            line.X = 0;
                            // Note: Full justification would require repositioning each word
                        }
                    }
                    break;

                default: // Left
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
        foreach (var line in lines)
        {
            var currentX = line.X;
            var charIndex = line.StartCharIndex;

            for (var i = 0; i < line.Text.Length; i++)
            {
                var c = line.Text[i];
                var charWidth = MeasureText(c.ToString(), node);

                line.CharacterPositions.Add(new LineBox.CharacterPosition
                {
                    CharIndex = charIndex + i,
                    X = currentX,
                    Y = line.Baseline
                });

                currentX += charWidth;
            }

            line.EndCharIndex = charIndex + line.Text.Length;
        }
    }

    /// <summary>
    /// Measures the width of text.
    /// </summary>
    private double MeasureText(string text, LayoutNode node)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        using var paint = new SKPaint { IsAntialias = true };
        using var font = new SKFont(CreateSkiaFont(node)) { Size = (float)node.FontSize };

        return font.MeasureText(text);
    }

    /// <summary>
    /// Creates a SkiaSharp font from layout node properties.
    /// </summary>
    private SKTypeface CreateSkiaFont(LayoutNode node)
    {
        var style = SKFontStyleWeight.Normal;
        if (node.FontBold)
            style = SKFontStyleWeight.Bold;

        var italic = node.FontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;

        return SKTypeface.FromFamilyName(node.FontFamily ?? _defaultFontFamily, style, SKFontStyleWidth.Normal, italic);
    }

    /// <summary>
    /// Data class for collecting inline content (text and inline elements).
    /// </summary>
    private class InlineContent
    {
        public string CombinedText { get; set; } = string.Empty;
        public List<(int startChar, int endChar, LayoutNode node)> Elements { get; set; } = new();
        public bool HasContent => !string.IsNullOrEmpty(CombinedText);
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
            else if (child.LayoutType == LayoutType.Inline)
            {
                // Collect text from inline element and track its position
                var text = CollectTextFromNode(child);
                var startChar = charIndex;
                textBuilder.Append(text);
                charIndex += text.Length;
                content.Elements.Add((startChar, charIndex, child));
            }
        }

        content.CombinedText = textBuilder.ToString();
        return content;
    }

    /// <summary>
    /// Layouts inline children across the broken lines, distributing them based on text position.
    /// </summary>
    private void LayoutInlineChildrenOnLines(List<LayoutNode> inlineChildren, List<LineBox> lines, InlineContent inlineContent, LayoutNode parentNode)
    {
        if (lines.Count == 0)
        {
            return;
        }

        var lineHeight = parentNode.FontSize * 1.2;
        var commonBaseline = parentNode.FontSize * 0.8;

        // Clear any old LineBoxes first
        foreach (var child in inlineChildren)
        {
            child.LineBoxes.Clear();
        }

        // Build a map of character ranges for each child
        var childRanges = new List<(int start, int end, LayoutNode child)>();

        int charIndex = 0;
        foreach (var child in inlineChildren)
        {
            if (child.HtmlNode?.Type == HtmlElementType.Text)
            {
                var text = child.HtmlNode.TextContent ?? string.Empty;
                childRanges.Add((charIndex, charIndex + text.Length, child));
                charIndex += text.Length;
            }
            else if (child.LayoutType == LayoutType.Inline)
            {
                var text = CollectTextFromNode(child);
                childRanges.Add((charIndex, charIndex + text.Length, child));
                charIndex += text.Length;
            }
        }

        // Layout each line and position inline children that belong on that line
        for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            var line = lines[lineIndex];
            var lineStartY = lineIndex * lineHeight;

            // Use the line's own character indices if available
            var lineStartChar = line.StartCharIndex >= 0 ? line.StartCharIndex : 0;
            var lineEndChar = line.EndCharIndex >= 0 ? line.EndCharIndex : (lineStartChar + (line.Text?.Length ?? 0));

            double currentX = 0;

            // Find and position all children that contribute to this line
            foreach (var (rangeStart, rangeEnd, child) in childRanges)
            {
                // Check if this child's text overlaps with the current line
                if (rangeEnd <= lineStartChar || rangeStart >= lineEndChar)
                {
                    // Child is not on this line, skip it
                    continue;
                }

                // This child contributes to this line
                if (child.HtmlNode?.Type == HtmlElementType.Text)
                {
                    var text = child.HtmlNode.TextContent ?? string.Empty;
                    var textWidth = MeasureText(text, parentNode);

                    // Position this text node on the current line
                    child.X = currentX;
                    child.Y = lineStartY;
                    child.Width = textWidth;
                    child.Height = lineHeight;

                    // Create or append LineBox for this text on this line
                    var lineBox = new LineBox
                    {
                        Text = text,
                        X = 0,
                        Y = 0,
                        Width = textWidth,
                        Height = lineHeight,
                        Baseline = commonBaseline
                    };
                    child.LineBoxes.Add(lineBox);

                    currentX += textWidth;
                }
                else if (child.LayoutType == LayoutType.Inline)
                {
                    // For inline elements, just measure the text width without recursing LayoutInlineNode
                    var text = CollectTextFromNode(child);
                    var textWidth = MeasureText(text, child);

                    // Position this inline element on the current line
                    child.X = currentX;
                    child.Y = lineStartY;
                    child.Width = textWidth;
                    child.Height = lineHeight;

                    // Create or append LineBox for this inline element on this line
                    var lineBox = new LineBox
                    {
                        Text = text,
                        X = 0,
                        Y = 0,
                        Width = textWidth,
                        Height = lineHeight,
                        Baseline = commonBaseline
                    };
                    child.LineBoxes.Add(lineBox);

                    currentX += textWidth;
                }
            }
        }
    }

}

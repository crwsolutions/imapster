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
            BorderBottomColor = mergedStyle.BorderBottomColor,
            Width = mergedStyle.WidthSet ? mergedStyle.Width : 0,
            WidthSet = mergedStyle.WidthSet
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
        else if (layoutNode.HtmlNode?.Type == HtmlElementType.LineBreak)
        {
            // BR elements create a line break with minimal height (just line height)
            // We use the font size as a reasonable line height
            layoutNode.Height = layoutNode.FontSize;
            layoutNode.Width = 0;
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

            // Position the child element
            child.X = CalculateBlockChildXPosition(node, child, contentWidth);
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

                // Distribute inline elements across lines based on text positions
                LayoutInlineChildrenOnLines(inlineChildren, lines, inlineContent, node);

                // Add style spans to parent lines to indicate which parts belong to which children
                BuildParentLineStyleSpans(lines, inlineContent, inlineChildren);

                // Always set node.LineBoxes for rendering, but children also have their own
                node.LineBoxes = lines;

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

        if (!hasNonEmptyContent)
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

    private void LayoutInlineNode(LayoutNode node, double availableWidth)
    {
        var lines = new List<LineBox>();
        var lineHeight = node.FontSize * 1.2;
        var currentX = 0.0;
        var currentY = 0.0;
        var currentLineWidth = 0.0;
        var currentLineText = new System.Text.StringBuilder();
        var currentLineSpans = new List<(string text, LayoutNode sourceNode)>();
        var charIndex = 0;
        var lineStartChar = 0;
        var innerWidth = (float)(availableWidth - node.PaddingLeft - node.PaddingRight - node.BorderLeftWidth - node.BorderRightWidth);

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
                    if (currentLineText.Length > 0)
                    {
                        var line = CreateLineBox(currentLineText.ToString(), currentLineSpans, currentLineWidth, currentY, lineStartChar, charIndex, lineHeight, node.FontSize);
                        lines.Add(line);
                    }

                    // Check if text fits on a new line by itself
                    if (textWidth <= innerWidth)
                    {
                        currentLineText.Clear();
                        currentLineSpans.Clear();
                        currentLineText.Append(text);
                        currentLineSpans.Add((text, child));
                        currentLineWidth = textWidth;
                        currentX = 0;
                        currentY += lineHeight;
                        lineStartChar = charIndex;
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
                            currentY += lineHeight;
                            lineStartChar = charIndex;
                            child.X = 0;
                            child.Y = currentY;
                            charIndex += prefix.Length;
                            
                            // Add remaining suffix to next line
                            if (suffix.Length > 0)
                            {
                                // This would require more complex handling for partial text nodes
                                // For now, we'll just add the suffix to the next line
                                currentLineText.Clear();
                                currentLineSpans.Clear();
                                currentLineText.Append(suffix);
                                currentLineSpans.Add((suffix, child));
                                currentLineWidth = MeasureText(suffix, node);
                                currentY += lineHeight;
                                lineStartChar = charIndex;
                            }
                        }
                        else
                        {
                            // Even the first character doesn't fit - force it anyway
                            currentLineText.Append(text);
                            currentLineSpans.Add((text, child));
                            currentLineWidth = textWidth;
                            currentY += lineHeight;
                            lineStartChar = charIndex;
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
                if (currentLineText.Length > 0)
                {
                    var line = CreateLineBox(currentLineText.ToString(), currentLineSpans, currentLineWidth, currentY, lineStartChar, charIndex, lineHeight, node.FontSize);
                    lines.Add(line);
                    currentLineText.Clear();
                    currentLineSpans.Clear();
                    currentLineWidth = 0;
                    lineStartChar = charIndex;
                }
                
                // Always add an empty line for the <br/> to preserve vertical spacing
                var emptyLine = CreateLineBox(string.Empty, new List<(string text, LayoutNode sourceNode)>(), 0, currentY, charIndex, charIndex, lineHeight, node.FontSize);
                lines.Add(emptyLine);
                currentY += lineHeight;
                
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
                    if (currentLineText.Length > 0)
                    {
                        var line = CreateLineBox(currentLineText.ToString(), currentLineSpans, currentLineWidth, currentY, lineStartChar, charIndex, lineHeight, node.FontSize);
                        lines.Add(line);
                    }
                    
                    currentLineText.Clear();
                    currentLineSpans.Clear();
                    currentLineWidth = 0;
                    currentX = 0;
                    currentY += lineHeight;
                    lineStartChar = charIndex;
                    child.X = 0;
                    child.Y = currentY;
                }
            }
        }

        // Add final line if there's remaining content
        if (currentLineText.Length > 0)
        {
            var line = CreateLineBox(currentLineText.ToString(), currentLineSpans, currentLineWidth, currentY, lineStartChar, charIndex, lineHeight, node.FontSize);
            lines.Add(line);
            currentY += lineHeight;
        }

        // Apply text alignment
        ApplyTextAlignment(lines, node, innerWidth);

        // Calculate character positions for hit testing
        CalculateCharacterPositions(lines, node);

        // Set node dimensions
        node.LineBoxes = lines;
        node.Height = lines.Count > 0 ? currentY : 0;
        node.Width = lines.Count > 0 ? lines.Max(l => l.Width) : 0;
    }

    /// <summary>
    /// Creates a LineBox with style spans tracking which nodes the text comes from.
    /// </summary>
    private LineBox CreateLineBox(string text, List<(string text, LayoutNode sourceNode)> spans, double width, double y, int startCharIndex, int endCharIndex, double lineHeight, double fontSize)
    {
        var line = new LineBox
        {
            Text = text,
            X = 0,
            Y = y,
            Width = width,
            Height = lineHeight,
            Baseline = fontSize,
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

        return line;
    }

    /// <summary>
    /// Measures the width of text using the specified font.
    /// </summary>
    private double MeasureText(string text, LayoutNode node)
    {
        var typeface = SKTypeface.FromFamilyName(node.FontFamily ?? _defaultFontFamily,
            node.FontBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            node.FontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
        using var font = new SKFont(typeface) { Size = (float)node.FontSize };
        return font.MeasureText(text);
    }

    /// <summary>
    /// Breaks a word into two parts that fit within the available width.
    /// </summary>
    private (string prefix, string suffix) BreakWord(string word, double availableWidth, LayoutNode node)
    {
        // Try to find a break point using hyphenation or character limits
        // For simplicity, we'll just return the first character as prefix if the word is too wide
        var firstChar = word.Substring(0, 1);
        var firstCharWidth = MeasureText(firstChar, node);
        
        if (firstCharWidth > availableWidth)
        {
            // Even the first character doesn't fit - return empty prefix and full word as suffix
            return (string.Empty, word);
        }

        // Binary search to find the longest prefix that fits
        int left = 1, right = word.Length;
        int bestFit = 1;

        while (left <= right)
        {
            int mid = (left + right) / 2;
            var wordPrefix = word.Substring(0, mid);
            var width = MeasureText(wordPrefix, node);

            if (width <= availableWidth)
            {
                bestFit = mid;
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }

        var prefix = word.Substring(0, bestFit);
        var suffix = word.Substring(bestFit);
        return (prefix, suffix);
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
                // Collect text from inline element and track its position
                var text = CollectTextFromNode(child);
                var startChar = charIndex;
                textBuilder.Append(text);
                charIndex += text.Length;
                content.Elements.Add((startChar, charIndex, child));
            }
        }

        content.CombinedText = textBuilder.ToString();
        content.HasContent = textBuilder.Length > 0;
        return content;
    }

    /// <summary>
    /// Breaks text into lines based on available width.
    /// </summary>
    private List<LineBox> BreakTextIntoLines(string text, LayoutNode node, double availableWidth)
    {
        var lines = new List<LineBox>();
        var lineHeight = node.FontSize * 1.2;
        var currentY = 0.0;
        var currentLineText = new System.Text.StringBuilder();
        var currentWidth = 0.0;
        var charIndex = 0;
        var lineStartChar = 0;

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
                var spaceWidth = MeasureText(" ", node);
                var totalWidth = currentWidth + wordWidth + (currentLineText.Length > 0 ? spaceWidth : 0);

                if (totalWidth <= availableWidth && currentLineText.Length > 0)
                {
                    // Word fits on current line
                    currentLineText.Append(" ").Append(word);
                    currentWidth = MeasureText(currentLineText.ToString(), node);
                }
                else
                {
                    // Start new line if current line has content
                    if (currentLineText.Length > 0)
                    {
                        var line = new LineBox
                        {
                            Text = currentLineText.ToString(),
                            X = 0,
                            Y = currentY,
                            Width = currentWidth,
                            Height = lineHeight,
                            Baseline = node.FontSize,
                            StartCharIndex = lineStartChar,
                            EndCharIndex = charIndex
                        };
                        lines.Add(line);

                        currentY += lineHeight;
                        lineStartChar = charIndex;
                    }

                    // Start new line with current word
                    currentLineText.Clear();
                    currentLineText.Append(word);
                    currentWidth = wordWidth;
                }

                charIndex += word.Length + 1; // +1 for space
            }

            // Handle end of segment (either end of text or newline)
            if (currentLineText.Length > 0)
            {
                var line = new LineBox
                {
                    Text = currentLineText.ToString(),
                    X = 0,
                    Y = currentY,
                    Width = currentWidth,
                    Height = lineHeight,
                    Baseline = node.FontSize,
                    StartCharIndex = lineStartChar,
                    EndCharIndex = charIndex
                };
                lines.Add(line);
                currentY += lineHeight;
                lineStartChar = charIndex;
                currentLineText.Clear();
                currentWidth = 0;
            }
            else if (segmentIndex < segments.Length - 1)
            {
                // Empty segment (consecutive <br/>) - add empty line to reserve vertical space
                var emptyLine = new LineBox
                {
                    Text = string.Empty,
                    X = 0,
                    Y = currentY,
                    Width = 0,
                    Height = lineHeight,
                    Baseline = node.FontSize,
                    StartCharIndex = charIndex,
                    EndCharIndex = charIndex
                };
                lines.Add(emptyLine);
                currentY += lineHeight;
            }

            charIndex++; // Account for newline character
            segmentIndex++;
        }

        return lines;
    }

    /// <summary>
    /// Layouts inline children across the broken lines, distributing them based on text position.
    /// </summary>
    private void LayoutInlineChildrenOnLines(List<LayoutNode> inlineChildren, List<LineBox> lines, InlineContent inlineContent, LayoutNode parentNode)
    {
        if (lines.Count == 0)
            return;

        var lineHeight = parentNode.FontSize * 1.2;

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
            else if (child.HtmlNode?.Type == HtmlElementType.LineBreak)
            {
                charIndex += 1;
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

            var lineStartChar = line.StartCharIndex >= 0 ? line.StartCharIndex : 0;
            var lineEndChar = line.EndCharIndex >= 0 ? line.EndCharIndex : (lineStartChar + (line.Text?.Length ?? 0));

            double currentX = 0;

            foreach (var (rangeStart, rangeEnd, child) in childRanges)
            {
                if (rangeEnd <= lineStartChar || rangeStart >= lineEndChar)
                    continue;

                if (child.HtmlNode?.Type == HtmlElementType.Text)
                {
                    var text = child.HtmlNode.TextContent ?? string.Empty;
                    var textWidth = MeasureText(text, parentNode);

                    child.X = currentX;
                    child.Y = lineStartY;
                    child.Width = textWidth;
                    child.Height = lineHeight;

                    var lineBox = new LineBox
                    {
                        Text = text,
                        X = 0,
                        Y = 0,
                        Width = textWidth,
                        Height = lineHeight,
                        Baseline = parentNode.FontSize
                    };

                    // Add style span pointing to parent node for text
                    lineBox.StyleSpans.Add(new LineBox.InlineStyleSpan
                    {
                        StartIndex = 0,
                        Length = text.Length,
                        SourceNode = parentNode
                    });

                    child.LineBoxes.Add(lineBox);

                    currentX += textWidth;
                }
                else if (child.LayoutType == LayoutType.Inline)
                {
                    var text = CollectTextFromNode(child);
                    var textWidth = MeasureText(text, child);

                    child.X = currentX;
                    child.Y = lineStartY;
                    child.Width = textWidth;
                    child.Height = lineHeight;

                    var lineBox = new LineBox
                    {
                        Text = text,
                        X = 0,
                        Y = 0,
                        Width = textWidth,
                        Height = lineHeight,
                        Baseline = parentNode.FontSize
                    };

                    // Add style span pointing to the child (link) node
                    lineBox.StyleSpans.Add(new LineBox.InlineStyleSpan
                    {
                        StartIndex = 0,
                        Length = text.Length,
                        SourceNode = child
                    });

                    child.LineBoxes.Add(lineBox);

                    currentX += textWidth;
                }
            }
        }
    }

    /// <summary>
    /// Builds style spans for parent lines to indicate which parts belong to which inline children.
    /// This allows proper rendering of styled inline elements like links.
    /// </summary>
    private void BuildParentLineStyleSpans(List<LineBox> parentLines, InlineContent inlineContent, List<LayoutNode> inlineChildren)
    {
        // Build the combined text and track character ranges for all children
        var combinedTextBuilder = new System.Text.StringBuilder();
        var allRanges = new List<(int start, int end, LayoutNode child, bool isInlineElement, string text)>();
        int charIndex = 0;

        foreach (var child in inlineChildren)
        {
            if (child.HtmlNode?.Type == HtmlElementType.Text)
            {
                var text = child.HtmlNode.TextContent ?? string.Empty;
                combinedTextBuilder.Append(text);
                allRanges.Add((charIndex, charIndex + text.Length, child, false, text));
                charIndex += text.Length;
            }
            else if (child.HtmlNode?.Type == HtmlElementType.LineBreak)
            {
                combinedTextBuilder.Append('\n');
                charIndex += 1;
            }
            else if (child.LayoutType == LayoutType.Inline)
            {
                // Inline element (like <a>, <strong>, etc.)
                var text = CollectTextFromNode(child);
                combinedTextBuilder.Append(text);
                allRanges.Add((charIndex, charIndex + text.Length, child, true, text));
                charIndex += text.Length;
            }
        }

        var combinedText = combinedTextBuilder.ToString();

        // For each parent line, build style spans by finding where the child text appears in the line
        foreach (var line in parentLines)
        {
            line.StyleSpans.Clear();

            if (line.Text == null || line.Text.Length == 0)
                continue;

            // Find the position of this line's text within the combined text
            // The line text should be a substring of the combined text (with spaces handled)
            int linePosInCombined = combinedText.IndexOf(line.Text);
            
            if (linePosInCombined == -1)
            {
                // Fallback: try to find the line text ignoring exact spacing
                // This handles cases where word wrapping might have altered spacing
                linePosInCombined = FindLinePositionInCombinedText(line.Text, combinedText, allRanges);
            }

            // For each inline element (link, strong, etc.), find where it appears in this line
            foreach (var (rangeStart, rangeEnd, child, isInlineElement, rangeText) in allRanges)
            {
                if (!isInlineElement || string.IsNullOrEmpty(rangeText))
                    continue;

                // Find where this child's text appears in the line text
                int spanStartInLine = line.Text.IndexOf(rangeText, StringComparison.Ordinal);
                
                if (spanStartInLine >= 0)
                {
                    // Found the text - add a style span for it
                    line.StyleSpans.Add(new LineBox.InlineStyleSpan
                    {
                        StartIndex = spanStartInLine,
                        Length = rangeText.Length,
                        SourceNode = child
                    });
                }
                else
                {
                    // Text not found exactly - might be split across lines or partial match
                    // Try to find partial overlap
                    int partialStart = FindPartialMatch(line.Text, rangeText, rangeStart, linePosInCombined);
                    if (partialStart >= 0)
                    {
                        int partialLength = Math.Min(rangeText.Length, line.Text.Length - partialStart);
                        line.StyleSpans.Add(new LineBox.InlineStyleSpan
                        {
                            StartIndex = partialStart,
                            Length = partialLength,
                            SourceNode = child
                        });
                    }
                }
            }
        }
    }

    /// <summary>
    /// Finds the position of a line within the combined text.
    /// </summary>
    private int FindLinePositionInCombinedText(string lineText, string combinedText, List<(int start, int end, LayoutNode child, bool isInlineElement, string text)> ranges)
    {
        // Try to find the line by matching the first word
        var firstWord = lineText.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (string.IsNullOrEmpty(firstWord))
            return -1;

        int pos = combinedText.IndexOf(firstWord);
        if (pos >= 0 && combinedText.Substring(pos, lineText.Length) == lineText)
            return pos;

        return -1;
    }

    /// <summary>
    /// Finds a partial match of rangeText in lineText based on character positions.
    /// </summary>
    private int FindPartialMatch(string lineText, string rangeText, int rangeStart, int linePosInCombined)
    {
        if (linePosInCombined < 0)
            return -1;

        // Calculate where the range text should appear in the line
        int expectedPos = rangeStart - linePosInCombined;
        
        if (expectedPos >= 0 && expectedPos < lineText.Length)
        {
            // Verify that the text at expectedPos matches the start of rangeText
            int matchLength = 0;
            while (matchLength < rangeText.Length && expectedPos + matchLength < lineText.Length)
            {
                if (lineText[expectedPos + matchLength] == rangeText[matchLength])
                    matchLength++;
                else
                    break;
            }
            
            if (matchLength > 0)
                return expectedPos;
        }

        return -1;
    }

    /// <summary>
    /// Helper class to hold inline content data.
    /// </summary>
    private class InlineContent
    {
        public string CombinedText { get; set; } = string.Empty;
        public bool HasContent { get; set; }
        public List<(int start, int end, LayoutNode child)> Elements { get; } = new();
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

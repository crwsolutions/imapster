using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.Text.RegularExpressions;

namespace Imapster.HtmlViewer.Parsing;

public sealed partial class HtmlParser
{
    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex CommentRegex { get; }

    private readonly AngleSharp.Html.Parser.HtmlParser _angleSharpParser = new();

    public HtmlNode Parse(string html)
    {
        var document = _angleSharpParser.ParseDocumentAsync(html).Result;
        var root = new HtmlNode { Type = HtmlElementType.DocumentFragment, TagName = "#document-fragment" };

        if (document.Body != null)
        {
            foreach (var child in document.Body.ChildNodes)
            {
                if (child is AngleSharp.Html.Dom.IHtmlElement e)
                {
                    ParseNode(e, root, isPreformatted: false);
                }
                else if (child is AngleSharp.Dom.IText t)
                {
                    var normalizedText = NormalizeWhitespace(t.Text ?? string.Empty, isPreformatted: false);
                    if (!string.IsNullOrWhiteSpace(normalizedText))
                    {
                        root.Children.Add(HtmlNode.CreateText(normalizedText));
                    }
                }
            }
        }

        // Clean up: remove text nodes that contain only whitespace
        // This prevents extra spacing around block elements and BR tags
        CleanWhitespaceNodes(root);

        return root;
    }

    private void ParseNode(IHtmlElement element, HtmlNode parentNode, bool isPreformatted = false)
    {
        var node = new HtmlNode
        {
            Type = MapElementType(element.TagName),
            TagName = element.TagName
        };

        ParseInlineStyles(element, node.Style, node.Type);
        ParseAttributes(element, node);
        ParseDeprecatedAttributes(element, node.Style);

        // Check if this is a preformatted element
        var isCurrentPreformatted = isPreformatted || node.Type == HtmlElementType.Pre;

        switch (node.Type)
        {
            case HtmlElementType.Link:
                node.Href = element.GetAttribute("href") ?? string.Empty;
                break;

            case HtmlElementType.Image:
                node.Src = element.GetAttribute("src") ?? string.Empty;
                node.Alt = element.GetAttribute("alt") ?? string.Empty;
                break;

            case HtmlElementType.ListItem when parentNode is { Type: HtmlElementType.OrderedList or HtmlElementType.UnorderedList }:
                // Count how many ListItem siblings are already in the parent (these are the previous siblings)
                node.ListItemIndex = parentNode.Children.Count(c => c.Type == HtmlElementType.ListItem) + 1;
                break;

            case HtmlElementType.Bold or HtmlElementType.Italic:
                node.Style ??= new HtmlStyle();
                break;

            case HtmlElementType.Underline:
                node.Style ??= new HtmlStyle();
                break;
        }

        switch (node.Type)
        {
            case HtmlElementType.Bold: node.Style.FontWeightBold = true; break;
            case HtmlElementType.Italic: node.Style.FontStyleItalic = true; break;
            case HtmlElementType.Underline: node.Style.TextDecoration = TextDecoration.Underline; break;
            case HtmlElementType.Center: 
                node.Style ??= new HtmlStyle();
                node.Style.TextAlign = TextAlignment.Center; 
                break;
        }

        foreach (var child in element.ChildNodes)
        {
            if (child is AngleSharp.Html.Dom.IHtmlElement ce)
            {
                ParseNode(ce, node, isCurrentPreformatted);
            }
            else if (child is AngleSharp.Dom.IText tn)
            {
                var normalizedText = NormalizeWhitespace(tn.Text ?? string.Empty, isCurrentPreformatted);
                // Treat &nbsp; (non-breaking space, U+00A0) as content, not whitespace
                // Don't filter out text that contains non-breaking spaces
                var hasContent = !string.IsNullOrEmpty(normalizedText) &&
                    (normalizedText.Contains('\u00A0') || !string.IsNullOrWhiteSpace(normalizedText));
                if (hasContent)
                {
                    node.Children.Add(HtmlNode.CreateText(normalizedText));
                }
            }
        }

        if (node.Children.Count == 0 && !string.IsNullOrWhiteSpace(element.TextContent))
        {
            var normalizedContent = NormalizeWhitespace(element.TextContent, isCurrentPreformatted);
            if (!string.IsNullOrEmpty(normalizedContent))
            {
                node.Children.Add(HtmlNode.CreateText(normalizedContent));
            }
        }

        node.Parent = parentNode;
        parentNode.Children.Add(node);
    }

    private void ParseAttributes(IHtmlElement element, HtmlNode node)
    {
        foreach (var attr in element.Attributes)
        {
            // Skip height attribute for img elements - it's often a very large value
            // and should be handled by the layout engine, not as a style property
            if (node.Type == HtmlElementType.Image && attr.Name.Equals("height", StringComparison.OrdinalIgnoreCase))
                continue;
            
            node.Attributes[attr.Name] = attr.Value;
        }
    }

    private void ParseInlineStyles(IHtmlElement element, HtmlStyle style, HtmlElementType nodeType = HtmlElementType.Unknown)
    {
        var styleAttr = element.GetAttribute("style");
        if (string.IsNullOrEmpty(styleAttr)) return;

        styleAttr = CommentRegex.Replace(styleAttr, string.Empty);

        foreach (var property in styleAttr.Split(';'))
        {
            var parts = property.Split(':', 2);
            if (parts.Length != 2) continue;

            var propertyName = parts[0].Trim().ToLowerInvariant();
            var propertyValue = parts[1].Trim();

            switch (propertyName)
            {
                case "font-family":
                    style.FontFamily = propertyValue.Trim('"', ' ');
                    break;

                case "font-size":
                    style.FontSize = propertyValue.ParseLength();
                    break;

                case "font-weight":
                    style.FontWeightBold = propertyValue is "bold" or "700" or "800" or "900";
                    break;

                case "font-style":
                    style.FontStyleItalic = propertyValue == "italic";
                    break;

                case "color":
                    style.Color = propertyValue.ConvertColorToHex();
                    break;

                case "background-color":
                    style.BackgroundColor = propertyValue.ConvertColorToHex();
                    break;

                case "text-decoration":
                    style.TextDecoration = propertyValue switch
                    {
                        "underline" => TextDecoration.Underline,
                        "line-through" => TextDecoration.LineThrough,
                        "blink" => TextDecoration.Blink,
                        _ => TextDecoration.None
                    };
                    break;

                case "text-align":
                    style.TextAlign = propertyValue switch
                    {
                        "right" => TextAlignment.Right,
                        "center" => TextAlignment.Center,
                        "justify" => TextAlignment.Justify,
                        _ => TextAlignment.Left
                    };
                    break;

                case "text-transform":
                    style.TextTransform = propertyValue;
                    break;

                case "text-indent":
                    style.TextIndentSet = true;
                    style.TextIndent = propertyValue.ParseLength();
                    break;

                case "margin-top":
                    style.MarginTopSet = true;
                    style.MarginTop = propertyValue.ParseLength();
                    break;

                case "margin-right":
                    style.MarginRightSet = true;
                    style.MarginRight = propertyValue.ParseLength();
                    break;

                case "margin-bottom":
                    style.MarginBottomSet = true;
                    style.MarginBottom = propertyValue.ParseLength();
                    break;

                case "margin-left":
                    style.MarginLeftSet = true;
                    style.MarginLeft = propertyValue.ParseLength();
                    break;

                case "padding-top":
                    style.PaddingTopSet = true;
                    style.PaddingTop = propertyValue.ParseLength();
                    break;

                case "padding-right":
                    style.PaddingRightSet = true;
                    style.PaddingRight = propertyValue.ParseLength();
                    break;

                case "padding-bottom":
                    style.PaddingBottomSet = true;
                    style.PaddingBottom = propertyValue.ParseLength();
                    break;

                case "padding-left":
                    style.PaddingLeftSet = true;
                    style.PaddingLeft = propertyValue.ParseLength();
                    break;

                case "width":
                    if (propertyValue != "auto" && !propertyValue.EndsWith("%", StringComparison.OrdinalIgnoreCase))
                    {
                        style.WidthSet = true;
                        style.Width = propertyValue.ParseLength();
                    }
                    break;

                case "height":
                    // Skip height style for image elements - it's often a very large value
                    // and should be handled by the layout engine, not as a style property
                    if (nodeType == HtmlElementType.Image)
                        break;
                    
                    if (propertyValue != "auto" && !propertyValue.EndsWith("%", StringComparison.OrdinalIgnoreCase))
                    {
                        style.HeightSet = true;
                        style.Height = propertyValue.ParseLength();
                    }
                    break;

                case "border-top-width":
                    style.BorderTopWidthSet = true;
                    style.BorderTopWidth = propertyValue.ParseLength();
                    break;

                case "border-right-width":
                    style.BorderRightWidthSet = true;
                    style.BorderRightWidth = propertyValue.ParseLength();
                    break;

                case "border-bottom-width":
                    style.BorderBottomWidthSet = true;
                    style.BorderBottomWidth = propertyValue.ParseLength();
                    break;

                case "border-left-width":
                    style.BorderLeftWidthSet = true;
                    style.BorderLeftWidth = propertyValue.ParseLength();
                    break;

                case "border-top-color":
                    style.BorderTopColor = propertyValue.ConvertColorToHex();
                    break;

                case "border-right-color":
                    style.BorderRightColor = propertyValue.ConvertColorToHex();
                    break;

                case "border-bottom-color":
                    style.BorderBottomColor = propertyValue.ConvertColorToHex();
                    break;

                case "border-left-color":
                    style.BorderLeftColor = propertyValue.ConvertColorToHex();
                    break;

                case "display":
                    style.Display = propertyValue;
                    break;

                case "position":
                    style.Position = propertyValue;
                    break;

                case "list-style-type":
                    style.ListStyleType = propertyValue;
                    break;

                case "list-style-position":
                    style.ListStylePositionSet = true;
                    style.ListStylePosition = propertyValue;
                    break;

                case "vertical-align":
                    style.VerticalAlign = propertyValue switch
                    {
                        "sub" => VerticalAlign.Sub,
                        "super" => VerticalAlign.Super,
                        "top" => VerticalAlign.Top,
                        "text-top" => VerticalAlign.TextTop,
                        "middle" => VerticalAlign.Middle,
                        "bottom" => VerticalAlign.Bottom,
                        "text-bottom" => VerticalAlign.TextBottom,
                        _ => VerticalAlign.Baseline
                    };
                    break;

                case "white-space":
                    style.WhiteSpace = propertyValue;
                    break;
            }
        }
    }

    /// <summary>
    /// Parses deprecated HTML attributes (like 'align') and converts them to CSS styles.
    /// This handles legacy HTML that uses deprecated attributes for styling.
    /// Only applies the deprecated attribute if the style hasn't been explicitly set.
    /// </summary>
    private void ParseDeprecatedAttributes(IHtmlElement element, HtmlStyle style)
    {
        // Handle deprecated 'align' attribute on td, th, div, p, etc.
        // Only apply if text-align hasn't been explicitly set via style attribute
        var alignAttr = element.GetAttribute("align");
        if (!string.IsNullOrEmpty(alignAttr))
        {
            var alignValue = alignAttr.ToLowerInvariant();
            Imapster.HtmlViewer.Parsing.TextAlignment alignment = alignValue switch
            {
                "left" => Imapster.HtmlViewer.Parsing.TextAlignment.Left,
                "center" => Imapster.HtmlViewer.Parsing.TextAlignment.Center,
                "right" => Imapster.HtmlViewer.Parsing.TextAlignment.Right,
                "justify" => Imapster.HtmlViewer.Parsing.TextAlignment.Justify,
                _ => Imapster.HtmlViewer.Parsing.TextAlignment.Left
            };

            // Only apply align attribute if text-align is still at its default (Left)
            // This prevents align from overriding explicit style attribute values
            if (alignment != Imapster.HtmlViewer.Parsing.TextAlignment.Left || alignValue == "left")
            {
                if (style.TextAlign == Imapster.HtmlViewer.Parsing.TextAlignment.Left)
                {
                    style.TextAlign = alignment;
                }
            }
        }
    }

    private HtmlElementType MapElementType(string tagName) => tagName.ToLowerInvariant() switch
    {
        "p" => HtmlElementType.Paragraph,
        "h1" or "h2" or "h3" or "h4" or "h5" or "h6" => HtmlElementType.Heading,
        "ul" => HtmlElementType.UnorderedList,
        "ol" => HtmlElementType.OrderedList,
        "li" => HtmlElementType.ListItem,
        "span" => HtmlElementType.Span,
        "a" => HtmlElementType.Link,
        "img" => HtmlElementType.Image,
        "strong" or "b" => HtmlElementType.Bold,
        "em" or "i" => HtmlElementType.Italic,
        "u" => HtmlElementType.Underline,
        "div" => HtmlElementType.Div,
        "pre" => HtmlElementType.Pre,
        "blockquote" => HtmlElementType.Blockquote,
        "br" => HtmlElementType.LineBreak,
        "hr" => HtmlElementType.HorizontalRule,
        "button" => HtmlElementType.Button,
        "input" => HtmlElementType.Input,
        "table" => HtmlElementType.Table,
        "thead" => HtmlElementType.TableHeader,
        "tbody" => HtmlElementType.TableBody,
        "tfoot" => HtmlElementType.TableFooter,
        "tr" => HtmlElementType.TableRow,
        "td" => HtmlElementType.TableCell,
        "th" => HtmlElementType.TableHeaderCell,
        "section" => HtmlElementType.Section,
        "article" => HtmlElementType.Article,
        "header" => HtmlElementType.Header,
        "footer" => HtmlElementType.Footer,
        "nav" => HtmlElementType.Nav,
        "main" => HtmlElementType.Main,
        "center" => HtmlElementType.Center,
        "code" => HtmlElementType.Code,
        "q" => HtmlElementType.Quote,
        "cite" => HtmlElementType.Cite,
        "mark" => HtmlElementType.Mark,
        "small" => HtmlElementType.Small,
        "sub" => HtmlElementType.Subscript,
        "sup" => HtmlElementType.Superscript,
        "del" => HtmlElementType.Del,
        "ins" => HtmlElementType.Ins,
        "abbr" => HtmlElementType.Abbrev,
        "address" => HtmlElementType.Address,
        "figure" => HtmlElementType.Figure,
        "figcaption" => HtmlElementType.Figcaption,
        "body" => HtmlElementType.Div,
        _ => HtmlElementType.Unknown
    };

    /// <summary>
    /// Removes text nodes that contain only whitespace (spaces).
    /// These typically come from formatting/indentation in the source HTML and don't contribute to content.
    /// Preserves non-breaking spaces (&nbsp;) as they are intentional content.
    /// </summary>
    private void CleanWhitespaceNodes(HtmlNode node)
    {
        for (int i = node.Children.Count - 1; i >= 0; i--)
        {
            var child = node.Children[i];

            // Remove text nodes that are only whitespace (but NOT non-breaking spaces)
            // Non-breaking spaces (U+00A0) are intentional content
            if (child.Type == HtmlElementType.Text && 
                !string.IsNullOrEmpty(child.TextContent) &&
                !child.TextContent.Contains('\u00A0') &&
                string.IsNullOrWhiteSpace(child.TextContent))
            {
                node.Children.RemoveAt(i);
            }
            else
            {
                // Recursively clean children
                CleanWhitespaceNodes(child);
            }
        }
    }

    /// <summary>
    /// Trims collapsible whitespace from text nodes according to HTML whitespace rules.
    /// Removes leading/trailing spaces from text nodes that come after/before block-level elements or BR tags.
    /// Only trims actual space characters (U+0020), not all whitespace, to preserve formatting intent.
    /// This ensures that formatted (indented/newline-separated) HTML renders the same as single-line HTML.
    /// </summary>
    private void TrimCollapsibleWhitespace(HtmlNode node)
    {
        // Process all children recursively
        for (int i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];

            // Check if this is a text node
            if (child.Type == HtmlElementType.Text && !string.IsNullOrEmpty(child.TextContent))
            {
                var originalContent = child.TextContent;

                // Check if we should trim leading whitespace
                // Trim only if this is the first child or the previous sibling is a block element or BR
                // AND the text starts with space characters
                if ((i == 0 || IsBlockElement(node.Children[i - 1]) || node.Children[i - 1].Type == HtmlElementType.LineBreak) &&
                    child.TextContent.StartsWith(" "))
                {
                    child.TextContent = child.TextContent.TrimStart(' ');
                }

                // Check if we should trim trailing whitespace
                // Trim only if this is the last child or the next sibling is a block element or BR
                // AND the text ends with space characters
                if ((i == node.Children.Count - 1 || IsBlockElement(node.Children[i + 1]) || node.Children[i + 1].Type == HtmlElementType.LineBreak) &&
                    child.TextContent.EndsWith(" "))
                {
                    child.TextContent = child.TextContent.TrimEnd(' ');
                }

                // Remove text nodes that became empty after trimming (but log if content changed unexpectedly)
                if (string.IsNullOrEmpty(child.TextContent) && !string.IsNullOrEmpty(originalContent))
                {
                    node.Children.RemoveAt(i);
                    i--;
                }
            }
            else
            {
                // Recursively process non-text children
                TrimCollapsibleWhitespace(child);
            }
        }
    }

    /// <summary>
    /// Determines if an HTML element is a block-level element.
    /// Block-level elements create new formatting contexts and boundaries where whitespace collapses.
    /// Note: LineBreak (BR) is not included as a block element for whitespace trimming purposes,
    /// since it's technically an inline element that should preserve surrounding whitespace handling.
    /// </summary>
    private bool IsBlockElement(HtmlNode node)
    {
        return node.Type switch
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
            HtmlElementType.DocumentFragment => true,
            _ => false
        };
    }

    /// <summary>
    /// Normalizes whitespace in HTML text nodes according to HTML standards.
    /// Collapses multiple spaces/newlines into single spaces but preserves intentional spaces.
    /// For preformatted text, preserves all whitespace including newlines.
    /// Handles leading/trailing whitespace at block boundaries appropriately.
    /// </summary>
    private string NormalizeWhitespace(string text, bool isPreformatted = false)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // For preformatted text, don't normalize - preserve all whitespace
        if (isPreformatted)
            return text;

        // Preserve non-breaking spaces (U+00A0) while normalizing other whitespace
        // First, temporarily replace non-breaking spaces with a placeholder
        var placeholder = "\u0000NBSP\u0000";
        var withPlaceholder = text.Replace("\u00A0", placeholder);

        // Replace any sequence of whitespace (including newlines, tabs) with a single space
        var normalized = Regex.Replace(withPlaceholder, @"\s+", " ");

        // Restore non-breaking spaces
        var result = normalized.Replace(placeholder, "\u00A0");

        // Return the normalized text - leading/trailing space trimming is handled at the
        // caller level where we know the context (block boundaries, adjacent elements)
        return result;
    }
}

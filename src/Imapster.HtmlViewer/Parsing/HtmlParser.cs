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
    /// Normalizes whitespace in HTML text nodes according to HTML standards.
    /// Collapses multiple spaces/newlines into single spaces but preserves intentional spaces.
    /// For preformatted text, preserves all whitespace including newlines.
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
        return normalized.Replace(placeholder, "\u00A0");
    }
}

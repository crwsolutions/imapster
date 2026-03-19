namespace Imapster.HtmlRendering.Parsing;

public sealed class HtmlParser
{
    private string _html = string.Empty;
    private int _position;
    private HtmlStyle _currentStyle = new();
    private Stack<HtmlStyle> _styleStack = new();
    private Stack<HtmlNode> _nodeStack = new();
    
    public HtmlNode Parse(string html)
    {
        _html = html;
        _position = 0;
        _currentStyle = new HtmlStyle();
        _styleStack = new Stack<HtmlStyle>();
        _nodeStack = new Stack<HtmlNode>();
        
        var root = new HtmlNode();
        _nodeStack.Push(root);
        
        while (_position < _html.Length)
        {
            if (_html[_position] == '<')
            {
                if (IsComment())
                {
                    SkipComment();
                }
                else if (IsClosingTag())
                {
                    ProcessClosingTag();
                }
                else
                {
                    ProcessTag();
                }
            }
            else
            {
                ProcessText();
            }
        }
        
        while (_nodeStack.Count > 1)
        {
            CloseAllTags();
        }
        
        return FlattenTree(root);
    }
    
    private bool IsComment()
    {
        return _html.Substring(_position, Math.Min(4, _html.Length - _position)) == "<!--";
    }
    
    private void SkipComment()
    {
        _position += 4;
        var endPos = _html.IndexOf("-->", _position);
        if (endPos != -1)
            _position = endPos + 3;
        else
            _position = _html.Length;
    }
    
    private bool IsClosingTag()
    {
        return _html.Substring(_position, Math.Min(2, _html.Length - _position)) == "</";
    }
    
    private void ProcessClosingTag()
    {
        _position += 2;
        var tagName = ReadTagName();
        
        if (_nodeStack.Count > 1)
        {
            _nodeStack.Pop();
            if (_styleStack.Count > 0)
            {
                _currentStyle = _styleStack.Pop();
            }
        }
    }
    
    private void ProcessTag()
    {
        _position++;
        var tagName = ReadTagName();
        var attributes = ReadAttributes();
        
        var style = ParseStyleAttributes(attributes);
        
        var node = CreateNode(tagName, style, attributes);
        
        if (node != null)
        {
            var parent = _nodeStack.Peek();
            parent.Children.Add(node);
            _nodeStack.Push(node);
            
            if (!IsSelfClosing(tagName, attributes))
            {
                _styleStack.Push(_currentStyle);
                _currentStyle = style;
            }
        }
    }
    
    private string ReadTagName()
    {
        var start = _position;
        while (_position < _html.Length && char.IsLetterOrDigit(_html[_position]))
        {
            _position++;
        }
        return _html.Substring(start, _position - start).ToLowerInvariant();
    }
    
    private Dictionary<string, string> ReadAttributes()
    {
        var attributes = new Dictionary<string, string>();
        
        while (_position < _html.Length && _html[_position] != '>' && _html[_position] != '/')
        {
            SkipWhitespace();
            if (_html[_position] == '>' || _html[_position] == '/')
                break;
                
            var attrName = ReadAttributeName();
            SkipWhitespace();
            
            if (_html[_position] == '=')
            {
                _position++;
                SkipWhitespace();
                var attrValue = ReadAttributeValue();
                attributes[attrName.ToLowerInvariant()] = attrValue;
            }
            else if (!string.IsNullOrEmpty(attrName))
            {
                attributes[attrName.ToLowerInvariant()] = "true";
            }
        }
        
        return attributes;
    }
    
    private string ReadAttributeName()
    {
        var start = _position;
        while (_position < _html.Length && _html[_position] != '=' && !char.IsWhiteSpace(_html[_position]) && _html[_position] != '>')
        {
            _position++;
        }
        return _html.Substring(start, _position - start);
    }
    
    private string ReadAttributeValue()
    {
        var quote = _html[_position];
        _position++;
        var start = _position;
        
        while (_position < _html.Length && _html[_position] != quote)
        {
            _position++;
        }
        
        var value = _html.Substring(start, _position - start);
        if (_position < _html.Length)
            _position++;
            
        return DecodeHtmlEntities(value);
    }
    
    private void ProcessText()
    {
        var start = _position;
        while (_position < _html.Length && _html[_position] != '<')
        {
            _position++;
        }
        
        var text = _html.Substring(start, _position - start);
        
        if (!string.IsNullOrWhiteSpace(text))
        {
            var parent = _nodeStack.Peek();
            var textNode = new HtmlNode(HtmlElementType.Text, _currentStyle, DecodeHtmlEntities(text));
            parent.Children.Add(textNode);
        }
    }
    
    private HtmlNode CreateNode(string tagName, HtmlStyle style, Dictionary<string, string> attributes)
    {
        return tagName switch
        {
            "p" => new HtmlNode(HtmlElementType.Paragraph, style),
            "div" => new HtmlNode(HtmlElementType.Div, style),
            "span" => new HtmlNode(HtmlElementType.Span, style),
            "strong" or "b" => new HtmlNode(HtmlElementType.Strong, style),
            "em" or "i" => new HtmlNode(HtmlElementType.Emphasis, style),
            "h1" => new HtmlNode(HtmlElementType.Heading1, style),
            "h2" => new HtmlNode(HtmlElementType.Heading2, style),
            "h3" => new HtmlNode(HtmlElementType.Heading3, style),
            "h4" => new HtmlNode(HtmlElementType.Heading4, style),
            "h5" => new HtmlNode(HtmlElementType.Heading5, style),
            "h6" => new HtmlNode(HtmlElementType.Heading6, style),
            "br" => new HtmlNode(HtmlElementType.LineBreak, style),
            "ul" or "ol" => new HtmlNode(HtmlElementType.List, style),
            "li" => new HtmlNode(HtmlElementType.ListItem, style),
            "a" => new HtmlNode(HtmlElementType.Link, style)
            {
                Url = attributes.TryGetValue("href", out var url) ? url : null
            },
            "img" => new HtmlNode(HtmlElementType.Image, style)
            {
                ImageSrc = attributes.TryGetValue("src", out var src) ? src : null
            },
            "blockquote" => new HtmlNode(HtmlElementType.Blockquote, style),
            "code" => new HtmlNode(HtmlElementType.Code, style),
            "hr" => new HtmlNode(HtmlElementType.HorizontalRule, style),
            _ => new HtmlNode(HtmlElementType.None, style)
        };
    }
    
    private HtmlStyle ParseStyleAttributes(Dictionary<string, string> attributes)
    {
        var style = new HtmlStyle();
        
        if (attributes.TryGetValue("style", out var styleAttr))
        {
            ParseInlineStyle(styleAttr, style);
        }
        
        if (attributes.TryGetValue("class", out var classAttr))
        {
            ParseClasses(classAttr, style);
        }
        
        return style;
    }
    
    private void ParseInlineStyle(string styleAttr, HtmlStyle style)
    {
        var parts = styleAttr.Split(';');
        foreach (var part in parts)
        {
            var colonIndex = part.IndexOf(':');
            if (colonIndex == -1) continue;
            
            var property = part.Substring(0, colonIndex).Trim().ToLowerInvariant();
            var value = part.Substring(colonIndex + 1).Trim();
            
            switch (property)
            {
                case "font-size":
                    style.FontSize = ParseFontSize(value);
                    break;
                case "font-weight":
                    style.IsBold = value.ToLowerInvariant() == "bold" || int.TryParse(value, out var weight) && weight >= 700;
                    break;
                case "font-style":
                    style.IsItalic = value.ToLowerInvariant() == "italic";
                    break;
                case "color":
                    style.Color = value;
                    break;
                case "background-color":
                    style.BackgroundColor = value;
                    break;
                case "text-align":
                    style.TextAlign = value;
                    break;
                case "text-decoration":
                    style.TextDecoration = value;
                    break;
                case "line-height":
                    style.LineHeight = float.TryParse(value, out var lh) ? lh : 1.5;
                    break;
                case "margin-top":
                    style.MarginTop = ParseLength(value);
                    break;
                case "margin-bottom":
                    style.MarginBottom = ParseLength(value);
                    break;
                case "padding-left":
                    style.PaddingLeft = ParseLength(value);
                    break;
                case "padding-right":
                    style.PaddingRight = ParseLength(value);
                    break;
            }
        }
    }
    
    private void ParseClasses(string classAttr, HtmlStyle style)
    {
        // Could map CSS classes to styles here
    }
    
    private double ParseFontSize(string value)
    {
        if (int.TryParse(value, out var size))
            return size;
            
        return value.ToLowerInvariant() switch
        {
            "small" => 12,
            "x-small" => 10,
            "medium" => 16,
            "large" => 20,
            "x-large" => 24,
            "xx-large" => 32,
            _ => 16
        };
    }
    
    private double ParseLength(string value)
    {
        if (int.TryParse(value, out var pixels))
            return pixels;
            
        if (value.EndsWith("px") && int.TryParse(value[..^2], out var px))
            return px;
            
        return 0;
    }
    
    private string DecodeHtmlEntities(string text)
    {
        return text
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&amp;", "&")
            .Replace("&quot;", "\"")
            .Replace("&#39;", "'")
            .Replace("&nbsp;", " ");
    }
    
    private bool IsSelfClosing(string tagName, Dictionary<string, string> attributes)
    {
        return tagName == "br" || tagName == "hr" || tagName == "img" ||
               attributes.ContainsKey("br") || attributes.ContainsKey("hr") ||
               attributes.ContainsKey("img");
    }
    
    private void SkipWhitespace()
    {
        while (_position < _html.Length && char.IsWhiteSpace(_html[_position]))
        {
            _position++;
        }
    }
    
    private void CloseAllTags()
    {
        while (_nodeStack.Count > 1)
        {
            _nodeStack.Pop();
            if (_styleStack.Count > 0)
                _styleStack.Pop();
        }
    }
    
    private HtmlNode FlattenTree(HtmlNode node)
    {
        if (node.Type == HtmlElementType.None && !node.HasImage && node.Children.Count == 1)
        {
            var child = node.Children[0];
            child = FlattenTree(child);
            if (child.Type == HtmlElementType.None)
                child = new HtmlNode(HtmlElementType.Div, child.Style, child.Text, child.Children);
            return child;
        }
        
        if (node.Children != null)
        {
            for (var i = 0; i < node.Children.Count; i++)
            {
                node.Children[i] = FlattenTree(node.Children[i]);
            }
        }
        
        MergeAdjacentTextNodes(node);
        AssignCharacterIndices(node);
        
        return node;
    }
    
    private void MergeAdjacentTextNodes(HtmlNode node)
    {
        if (node.Children == null || node.Children.Count == 0)
            return;
            
        var merged = new List<HtmlNode>();
        HtmlNode? currentTextNode = null;
        
        foreach (var child in node.Children)
        {
            if (child.IsText)
            {
                if (currentTextNode == null)
                    currentTextNode = child;
                else
                    currentTextNode = currentTextNode with { Text = currentTextNode.Text + child.Text };
            }
            else
            {
                if (currentTextNode != null)
                {
                    merged.Add(currentTextNode);
                    currentTextNode = null;
                }
                merged.Add(child);
            }
        }
        
        if (currentTextNode != null)
            merged.Add(currentTextNode);
            
        node = node with { Children = merged };
    }
    
    private void AssignCharacterIndices(HtmlNode node)
    {
        if (node.IsText && !string.IsNullOrEmpty(node.Text))
        {
            node = node with { CharacterStart = node.CharacterStart, CharacterEnd = node.CharacterStart + node.Text!.Length };
        }
        else if (node.IsContainer && node.Children != null)
        {
            var start = node.Children.Count > 0 ? node.Children[0].CharacterStart : 0;
            var end = node.Children[node.Children.Count - 1].CharacterEnd;
            node = node with { CharacterStart = start, CharacterEnd = end };
        }
    }
}
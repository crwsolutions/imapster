namespace Imapster.HtmlRendering.Layout;

public sealed class LayoutEngine
{
    private readonly ITextMeasureService _textMeasureService;
    private readonly double _maxWidth;
    
    public LayoutEngine(ITextMeasureService textMeasureService, double maxWidth = double.MaxValue)
    {
        _textMeasureService = textMeasureService;
        _maxWidth = maxWidth;
    }
    
    public LayoutNode BuildLayoutTree(Parsing.HtmlNode htmlNode)
    {
        var layoutNode = ConvertNode(htmlNode);
        LayoutLines(layoutNode, _maxWidth);
        PositionLines(layoutNode);
        return layoutNode;
    }
    
    private LayoutNode ConvertNode(Parsing.HtmlNode node)
    {
        var children = node.Children?.Select(ConvertNode).ToList() ?? [];
        
        return new LayoutNode(
            Text: node.Text,
            Width: 0,
            Height: 0,
            X: 0,
            Y: 0,
            Previous: null,
            Next: null,
            Parent: null,
            Children: children,
            IsText: node.IsText,
            HasImage: node.HasImage,
            ImageSrc: node.ImageSrc,
            ImageData: node.ImageData,
            FontSize: node.Style.FontSize,
            IsBold: node.Style.IsBold,
            IsItalic: node.Style.IsItalic,
            FontFamily: node.Style.FontFamily,
            Color: node.Style.Color,
            BackgroundColor: node.Style.BackgroundColor,
            LineHeight: node.Style.LineHeight,
            MarginTop: node.Style.MarginTop,
            MarginBottom: node.Style.MarginBottom,
            PaddingLeft: node.Style.PaddingLeft,
            PaddingRight: node.Style.PaddingRight,
            TextAlign: node.Style.TextAlign,
            TextDecoration: node.Style.TextDecoration,
            Url: node.Style.Url,
            CharacterStart: node.CharacterStart,
            CharacterEnd: node.CharacterEnd
        );
    }
    
    private LayoutNode LayoutLines(LayoutNode node, double availableWidth)
    {
        if (node.IsText && !string.IsNullOrEmpty(node.Text))
        {
            var style = new TextStyle
            {
                FontSize = (float)node.FontSize,
                IsBold = node.IsBold,
                IsItalic = node.IsItalic,
                FontFamily = node.FontFamily,
                Color = node.Color,
                BackgroundColor = node.BackgroundColor,
                LineHeight = (float)node.LineHeight,
                TextAlign = node.TextAlign,
                TextDecoration = node.TextDecoration
            };
            
            var (width, height) = _textMeasureService.MeasureTextWithBounds(node.Text!, style);
            node.Width = Math.Min(width, availableWidth);
            node.Height = height * (float)node.LineHeight;
        }
        else if (node.IsContainer && node.Children != null)
        {
            var currentX = 0.0;
            var maxHeight = 0.0;
            
            foreach (var child in node.Children)
            {
                LayoutLines(child, availableWidth - currentX);
                
                currentX += child.Width + child.PaddingLeft + child.PaddingRight;
                maxHeight = Math.Max(maxHeight, child.Height);
            }
            
            node.Width = Math.Min(currentX, availableWidth);
            node.Height = maxHeight;
        }
        
        return node;
    }
    
    private void PositionLines(LayoutNode node)
    {
        if (node.IsText)
        {
            node.X = node.PaddingLeft;
            node.Y = 0;
        }
        else if (node.IsContainer && node.Children != null)
        {
            var currentX = node.PaddingLeft;
            var currentY = 0.0;
            var lineHeight = 0.0;
            
            foreach (var child in node.Children)
            {
                if (child.IsText)
                {
                    child.X = currentX;
                    child.Y = currentY;
                    lineHeight = Math.Max(lineHeight, child.Height);
                    
                    currentX += child.Width;
                    
                    if (currentX > _maxWidth - child.PaddingRight)
                    {
                        currentX = node.PaddingLeft;
                        currentY += lineHeight;
                        lineHeight = 0;
                    }
                }
                else
                {
                    PositionLines(child);
                    child.X = currentX;
                    child.Y = currentY;
                    lineHeight = Math.Max(lineHeight, child.Height);
                    currentX += child.Width;
                }
            }
            
            node.Height = currentY + lineHeight;
        }
    }
}
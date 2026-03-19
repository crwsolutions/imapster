namespace Imapster.HtmlRendering.Layout;

public sealed class HitTestEngine
{
    public HitTestResult HitTest(LayoutNode root, Point point)
    {
        var result = FindNodeAtPoint(root, point);
        
        if (result == null)
            return new HitTestResult(null, -1, -1);
            
        var charIndex = FindCharacterIndexAtPoint(result, point);
        
        return new HitTestResult(result, charIndex, GetCharacterOffset(result, charIndex));
    }
    
    private LayoutNode? FindNodeAtPoint(LayoutNode node, Point point)
    {
        if (node == null)
            return null;
            
        if (node.BoundingBox.Contains(point))
        {
            if (node.IsContainer && node.Children != null)
            {
                for (var i = node.Children.Count - 1; i >= 0; i--)
                {
                    var child = FindNodeAtPoint(node.Children[i], point);
                    if (child != null)
                        return child;
                }
            }
            
            return node;
        }
        
        if (node.IsContainer && node.Children != null)
        {
            for (var i = node.Children.Count - 1; i >= 0; i--)
            {
                var child = FindNodeAtPoint(node.Children[i], point);
                if (child != null)
                    return child;
            }
        }
        
        return null;
    }
    
    private int FindCharacterIndexAtPoint(LayoutNode node, Point point)
    {
        if (!node.IsText || string.IsNullOrEmpty(node.Text))
            return 0;
            
        var textWidth = node.Width;
        var charWidth = textWidth / Math.Max(node.Text!.Length, 1);
        var relativeX = point.X - node.X;
        
        if (relativeX <= 0)
            return 0;
        if (relativeX >= textWidth)
            return node.Text.Length;
            
        return (int)(relativeX / charWidth);
    }
    
    private int GetCharacterOffset(LayoutNode node, int index)
    {
        if (!node.IsText || node.CharacterStart == 0 && node.CharacterEnd == 0)
            return index;
            
        return node.CharacterStart + Math.Min(index, node.Text!.Length);
    }
}

public record HitTestResult(LayoutNode? Node, int CharIndex, int CharacterOffset);
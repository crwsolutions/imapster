namespace Imapster.HtmlRendering.Interaction;

using Imapster.HtmlRendering.Layout;

public sealed class LinkHandler
{
    private readonly List<(LayoutNode Node, string Url)> _links = new();
    
    public IReadOnlyList<(LayoutNode Node, string Url)> Links => _links;
    public int LinkCount => _links.Count;
    
    public void BuildLinks(LayoutNode root)
    {
        _links.Clear();
        FindLinks(root);
    }
    
    private void FindLinks(LayoutNode node)
    {
        if (node.IsContainer && node.Children != null)
        {
            foreach (var child in node.Children)
            {
                FindLinks(child);
            }
        }
        
        if (!string.IsNullOrEmpty(node.Url) && node.IsContainer)
        {
            _links.Add((node, node.Url!));
        }
    }
    
    public string? GetUrlAtPoint(Point point)
    {
        foreach (var (node, url) in _links)
        {
            if (node.BoundingBox.Contains(point))
                return url;
        }
        
        return null;
    }
}
namespace Imapster.HtmlRendering.Interaction;

public record SelectionRange(int Start, int End)
{
    public bool IsValid => Start >= 0 && End >= 0 && Start != End;
    public int Length => Math.Abs(End - Start);
    public SelectionRange Normalize() => Start < End ? this : new SelectionRange(End, Start);
};

public sealed class SelectionService
{
    private SelectionRange _selection = new(-1, -1);
    private readonly List<(int Start, int End, string Text)> _textRuns = new();
    
    public SelectionRange Selection => _selection;
    public bool HasSelection => _selection.IsValid;
    
    public void BuildTextRuns(Layout.LayoutNode root)
    {
        _textRuns.Clear();
        CollectTextRuns(root);
    }
    
    private void CollectTextRuns(Layout.LayoutNode node)
    {
        if (node.IsText && !string.IsNullOrEmpty(node.Text))
        {
            _textRuns.Add((node.CharacterStart, node.CharacterEnd, node.Text!));
        }
        
        if (node.IsContainer && node.Children != null)
        {
            foreach (var child in node.Children)
            {
                CollectTextRuns(child);
            }
        }
    }
    
    public void SetSelection(int start, int end)
    {
        _selection = new SelectionRange(start, end);
    }
    
    public void ClearSelection()
    {
        _selection = new SelectionRange(-1, -1);
    }
    
    public string? GetSelectedText()
    {
        if (!_selection.IsValid)
            return null;
            
        var normalized = _selection.Normalize();
        var selectedTexts = new List<string>();
        
        foreach (var (start, end, text) in _textRuns)
        {
            if (end <= normalized.Start || start >= normalized.End)
                continue;
                
            var runStart = Math.Max(start, normalized.Start);
            var runEnd = Math.Min(end, normalized.End);
            var index = runStart - start;
            var length = runEnd - runStart;
            
            if (length > 0)
                selectedTexts.Add(text.Substring(index, length));
        }
        
        return string.Join("", selectedTexts);
    }
    
    public Layout.LayoutNode? GetNodeAtSelectionStart()
    {
        if (!_selection.IsValid)
            return null;
            
        var normalized = _selection.Normalize();
        
        foreach (var (start, end, _) in _textRuns)
        {
            if (start <= normalized.Start && end >= normalized.Start)
                return FindNodeByTextRange(start, end);
        }
        
        return null;
    }
    
    private Layout.LayoutNode? FindNodeByTextRange(int start, int end)
    {
        // This would need to be passed in or built during layout
        return null;
    }
}
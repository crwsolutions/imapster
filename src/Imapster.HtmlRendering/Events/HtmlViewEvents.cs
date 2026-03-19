namespace Imapster.HtmlRendering.Events;

public sealed class HtmlViewLinkClickedEventArgs : EventArgs
{
    public string Link { get; }
    
    public HtmlViewLinkClickedEventArgs(string link)
    {
        Link = link;
    }
}

public sealed class HtmlViewTextSelectedEventArgs : EventArgs
{
    public string SelectedText { get; }
    
    public HtmlViewTextSelectedEventArgs(string selectedText)
    {
        SelectedText = selectedText;
    }
}

public sealed class HtmlViewSelectionChangedEventArgs : EventArgs
{
    public int StartIndex { get; }
    public int EndIndex { get; }
    
    public HtmlViewSelectionChangedEventArgs(int startIndex, int endIndex)
    {
        StartIndex = startIndex;
        EndIndex = endIndex;
    }
}
namespace Imapster.HtmlRendering.Events;

/// <summary>
/// Event arguments for selection changed events.
/// </summary>
public sealed class SelectionChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the start index of the selection.
    /// </summary>
    public int StartIndex { get; }

    /// <summary>
    /// Gets the end index of the selection.
    /// </summary>
    public int EndIndex { get; }

    /// <summary>
    /// Gets the selected text.
    /// </summary>
    public string SelectedText { get; }

    /// <summary>
    /// Gets whether the selection is empty.
    /// </summary>
    public bool IsEmpty => StartIndex < 0 || EndIndex <= StartIndex;

    public SelectionChangedEventArgs(int startIndex, int endIndex, string selectedText)
    {
        StartIndex = startIndex;
        EndIndex = endIndex;
        SelectedText = selectedText;
    }
}
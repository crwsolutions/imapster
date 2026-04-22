namespace Imapster.HtmlViewer.Layout;

/// <summary>
/// Represents a line of text in the layout tree with character positions.
/// </summary>
public sealed class LineBox
{
    /// <summary>
    /// Gets or sets the text content of the line.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the X position of the line.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Gets or sets the Y position of the line.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Gets or sets the width of the line.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the line.
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Gets or sets the baseline position.
    /// </summary>
    public double Baseline { get; set; }

    /// <summary>
    /// Gets or sets the starting character index in the source text.
    /// </summary>
    public int StartCharIndex { get; set; }

    /// <summary>
    /// Gets or sets the ending character index in the source text.
    /// </summary>
    public int EndCharIndex { get; set; }

    /// <summary>
    /// Gets or sets the character positions for hit testing.
    /// </summary>
    public List<CharacterPosition> CharacterPositions { get; set; } = [];

    /// <summary>
    /// Gets or sets whether this line is part of a selection.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Gets or sets the selection start within this line.
    /// </summary>
    public int? SelectionStart { get; set; }

    /// <summary>
    /// Gets or sets the selection end within this line.
    /// </summary>
    public int? SelectionEnd { get; set; }

    /// <summary>
    /// Gets or sets whether this line represents an explicit line break (<br/> element).
    /// </summary>
    public bool IsLineBreak { get; set; }

    /// <summary>
    /// Gets or sets the inline style spans for this line.
    /// Each span represents a range of characters with the same inline styling.
    /// </summary>
    public List<InlineStyleSpan> StyleSpans { get; set; } = [];

    /// <summary>
    /// Represents a character position for hit testing.
    /// </summary>
    public sealed class CharacterPosition
    {
        /// <summary>
        /// Gets or sets the character index.
        /// </summary>
        public int CharIndex { get; set; }

        /// <summary>
        /// Gets or sets the X position of the character.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Gets or sets the Y position of the character baseline.
        /// </summary>
        public double Y { get; set; }
    }

    /// <summary>
    /// Represents a span of characters with the same inline styling.
    /// </summary>
    public sealed class InlineStyleSpan
    {
        /// <summary>
        /// Gets or sets the start index of this span within the line text (0-based).
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// Gets or sets the length of this span in characters.
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Gets or sets the source node that provides the styling for this span.
        /// </summary>
        public LayoutNode? SourceNode { get; set; }
    }
}
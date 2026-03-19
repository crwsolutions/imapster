namespace Imapster.HtmlRendering.Parsing;

public record HtmlNode(
    HtmlElementType Type,
    HtmlStyle Style,
    string? Text = null,
    List<HtmlNode> Children = null!,
    string? ImageSrc = null,
    string? ImageData = null
)
{
    public HtmlNode() : this(HtmlElementType.None, new HtmlStyle()) { }
    
    public HtmlNode(HtmlElementType type, HtmlStyle style, string text)
        : this(type, style, text, []) { }
    
    public HtmlNode(HtmlElementType type, HtmlStyle style, List<HtmlNode> children)
        : this(type, style, null, children) { }
    
    public int CharacterStart { get; set; } = 0;
    public int CharacterEnd { get; set; } = 0;
    
    public bool IsText => Type == HtmlElementType.Text;
    public bool IsContainer => Children != null && Children.Count > 0;
    public bool HasImage => ImageSrc != null || ImageData != null;
}
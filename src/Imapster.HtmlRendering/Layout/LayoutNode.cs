namespace Imapster.HtmlRendering.Layout;

public class LayoutNode
{
    public string? Text { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public LayoutNode? Previous { get; set; }
    public LayoutNode? Next { get; set; }
    public LayoutNode? Parent { get; set; }
    public List<LayoutNode> Children { get; set; } = [];
    public bool IsText { get; set; }
    public bool HasImage { get; set; }
    public string? ImageSrc { get; set; }
    public string? ImageData { get; set; }
    public double FontSize { get; set; } = 16;
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public string? FontFamily { get; set; }
    public string? Color { get; set; }
    public string? BackgroundColor { get; set; }
    public double LineHeight { get; set; } = 1.5;
    public double MarginTop { get; set; }
    public double MarginBottom { get; set; }
    public double PaddingLeft { get; set; }
    public double PaddingRight { get; set; }
    public string? TextAlign { get; set; }
    public string? TextDecoration { get; set; }
    public string? Url { get; set; }
    public int CharacterStart { get; set; }
    public int CharacterEnd { get; set; }
    
    public Rect BoundingBox => new Rect(X, Y, Width, Height);
    public bool IsContainer => Children.Count > 0;
    
    public LayoutNode()
    {
        Text = null;
        Width = 0;
        Height = 0;
        X = 0;
        Y = 0;
        Previous = null;
        Next = null;
        Parent = null;
        Children = [];
        IsText = false;
        HasImage = false;
        ImageSrc = null;
        ImageData = null;
        FontSize = 16;
        IsBold = false;
        IsItalic = false;
        FontFamily = null;
        Color = null;
        BackgroundColor = null;
        LineHeight = 1.5;
        MarginTop = 0;
        MarginBottom = 0;
        PaddingLeft = 0;
        PaddingRight = 0;
        TextAlign = null;
        TextDecoration = null;
        Url = null;
        CharacterStart = 0;
        CharacterEnd = 0;
    }
}

public class Rect
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    
    public Rect(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
    
    public bool Contains(double x, double y) => x >= X && x <= X + Width && y >= Y && y <= Y + Height;
    public bool Contains(Point point) => Contains(point.X, point.Y);
}

public class Point
{
    public double X { get; set; }
    public double Y { get; set; }
    
    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }
}
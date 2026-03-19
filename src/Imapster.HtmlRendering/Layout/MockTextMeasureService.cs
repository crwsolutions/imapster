namespace Imapster.HtmlRendering.Layout;

public sealed class MockTextMeasureService : ITextMeasureService
{
    public float MeasureText(string text, TextStyle style)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
            
        return (float)(text.Length * style.FontSize * 0.6);
    }
    
    public (float width, float height) MeasureTextWithBounds(string text, TextStyle style)
    {
        if (string.IsNullOrEmpty(text))
            return (0, style.LineHeight);
            
        var width = (float)(text.Length * style.FontSize * 0.6);
        var height = style.LineHeight;
        
        return (width, height);
    }
    
    public float MeasureCharacterWidth(char character, TextStyle style)
    {
        return (float)(style.FontSize * 0.6);
    }
}
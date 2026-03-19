namespace Imapster.HtmlRendering.Layout;

public interface ITextMeasureService
{
    float MeasureText(string text, TextStyle style);
    (float width, float height) MeasureTextWithBounds(string text, TextStyle style);
    float MeasureCharacterWidth(char character, TextStyle style);
}
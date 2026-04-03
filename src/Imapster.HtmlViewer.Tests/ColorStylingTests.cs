using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

/// <summary>
/// Tests to verify that CSS colors are properly parsed and applied to elements.
/// </summary>
public class ColorStylingTests
{
    private readonly HtmlParser _htmlParser;
    private readonly LayoutEngine _layoutEngine;

    public ColorStylingTests()
    {
        _htmlParser = new HtmlParser();
        _layoutEngine = new LayoutEngine();
    }

    [Fact]
    public void TextColor_FromInlineStyle_IsParsed()
    {
        // Arrange
        var html = "<p style='color: #333333;'>Dark gray text</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotEmpty(layoutRoot.Children);
        var paragraph = layoutRoot.Children[0];

        // The paragraph's text color should be set to dark gray
        Assert.NotNull(paragraph.TextColor);
        Assert.Equal("#333333", paragraph.TextColor);
    }

    [Fact]
    public void BackgroundColor_FromInlineStyle_IsParsed()
    {
        // Arrange
        var html = "<p style='background-color: #f0f0f0;'>Light gray background</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotEmpty(layoutRoot.Children);
        var paragraph = layoutRoot.Children[0];

        // The paragraph's background color should be set
        Assert.NotNull(paragraph.BackgroundColor);
        Assert.Equal("#f0f0f0", paragraph.BackgroundColor);
    }

    [Fact]
    public void HeadingTextColor_FromInlineStyle_IsParsed()
    {
        // Arrange
        var html = "<h1 style='color: #333333;'>Welcome</h1>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotEmpty(layoutRoot.Children);
        var heading = layoutRoot.Children[0];

        // The heading's text color should be set
        Assert.NotNull(heading.TextColor);
        Assert.Equal("#333333", heading.TextColor);
    }

    [Fact]
    public void MultipleColors_OnDifferentElements_AllParsed()
    {
        // Arrange
        var html = @"<div>
                    <h1 style='color: #333333;'>Heading</h1>
                    <p style='color: #666666;'>Text</p>
                    <p style='background-color: #f0f0f0;'>Background</p>
                </div>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - find the div
        Assert.NotEmpty(layoutRoot.Children);
        var div = layoutRoot.Children[0];

        // Find heading, paragraphs
        var heading = div.Children.FirstOrDefault(c => c.HtmlNode?.Type == HtmlElementType.Heading);
        var textPara = div.Children.FirstOrDefault(c =>
            c.HtmlNode?.Type == HtmlElementType.Paragraph && c.TextColor == "#666666");
        var bgPara = div.Children.FirstOrDefault(c =>
            c.HtmlNode?.Type == HtmlElementType.Paragraph && c.BackgroundColor == "#f0f0f0");

        Assert.NotNull(heading);
        Assert.Equal("#333333", heading.TextColor);

        Assert.NotNull(textPara);
        Assert.Equal("#666666", textPara.TextColor);

        Assert.NotNull(bgPara);
        Assert.Equal("#f0f0f0", bgPara.BackgroundColor);
    }

    [Fact]
    public void RgbColor_IsConvertedToHex()
    {
        // Arrange
        var html = "<p style='color: rgb(51, 51, 51);'>RGB Color</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotEmpty(layoutRoot.Children);
        var paragraph = layoutRoot.Children[0];

        // RGB(51, 51, 51) should be converted to #333333
        Assert.NotNull(paragraph.TextColor);
        Assert.Equal("#333333", paragraph.TextColor);
    }

    [Fact]
    public void NamedColor_IsConvertedToHex()
    {
        // Arrange
        var html = "<p style='color: gray;'>Gray text</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotEmpty(layoutRoot.Children);
        var paragraph = layoutRoot.Children[0];

        // "gray" should be converted to #808080
        Assert.NotNull(paragraph.TextColor);
        Assert.Equal("#808080", paragraph.TextColor);
    }
}

using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

/// <summary>
/// Tests to verify that styling is properly rendered to canvas.
/// </summary>
public class BoldItalicRenderingTests
{
    private readonly HtmlParser _htmlParser;
    private readonly LayoutEngine _layoutEngine;

    public BoldItalicRenderingTests()
    {
        _htmlParser = new HtmlParser();
        _layoutEngine = new LayoutEngine();
    }

    [Fact]
    public void BoldElement_TextNodeRendersWithBoldStyling()
    {
        // Arrange
        var html = "<p>This is <b>bold</b> text</p>";
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Find the bold element
        var paragraph = layoutRoot.Children[0];
        var boldElement = paragraph.Children.FirstOrDefault(c => c.HtmlNode?.Type == HtmlElementType.Bold);
        Assert.NotNull(boldElement);

        // Find the text child of the bold element
        Assert.NotEmpty(boldElement.Children);
        var textNode = boldElement.Children[0];
        Assert.Equal(HtmlElementType.Text, textNode.HtmlNode?.Type);

        // The text node should have LineBoxes with the bold text
        Assert.NotEmpty(textNode.LineBoxes);
        var lineBox = textNode.LineBoxes[0];
        Assert.Equal("bold", lineBox.Text);

        // When we render with boldElement as parent, FontBold should be true
        Assert.True(boldElement.FontBold, "Bold element should have FontBold = true, which will be passed to text rendering");
    }

    [Fact]
    public void TextBetweenInlineElements_PreservesText()
    {
        // Arrange
        var html = "<p>Before <b>bold</b> middle <i>italic</i> after</p>";
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Find the paragraph
        var paragraph = layoutRoot.Children[0];

        // Verify structure: Text, Bold, Text, Italic, Text
        Assert.Equal(5, paragraph.Children.Count);

        var textBefore = paragraph.Children[0];
        var boldElement = paragraph.Children[1];
        var textMiddle = paragraph.Children[2];
        var italicElement = paragraph.Children[3];
        var textAfter = paragraph.Children[4];

        // Verify text content
        Assert.Equal("Before ", textBefore.HtmlNode?.TextContent);
        Assert.Equal(" middle ", textMiddle.HtmlNode?.TextContent);
        Assert.Equal(" after", textAfter.HtmlNode?.TextContent);

        // Verify styling
        Assert.True(boldElement.FontBold);
        Assert.True(italicElement.FontItalic);
    }
}

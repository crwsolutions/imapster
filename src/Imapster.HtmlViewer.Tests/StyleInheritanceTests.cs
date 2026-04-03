using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

/// <summary>
/// Tests to verify that styling (bold, italic) is properly inherited and applied.
/// </summary>
public class StyleInheritanceTests
{
    private readonly HtmlParser _htmlParser;
    private readonly LayoutEngine _layoutEngine;

    public StyleInheritanceTests()
    {
        _htmlParser = new HtmlParser();
        _layoutEngine = new LayoutEngine();
    }

    [Fact]
    public void BoldElement_HasFontBoldProperty()
    {
        // Arrange
        var html = "<p>This is <b>bold</b> text</p>";
        
        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);
        
        // Find the paragraph
        Assert.NotEmpty(layoutRoot.Children);
        var paragraph = layoutRoot.Children[0];
        
        // Find the bold element (should be second child: Text, Bold, Text)
        Assert.NotEmpty(paragraph.Children);
        var boldElement = paragraph.Children.FirstOrDefault(c => c.HtmlNode?.Type == HtmlElementType.Bold);
        Assert.NotNull(boldElement);
        
        // Assert that the bold element has FontBold = true
        Assert.True(boldElement.FontBold, "Bold element should have FontBold property set to true");
    }

    [Fact]
    public void ItalicElement_HasFontItalicProperty()
    {
        // Arrange
        var html = "<p>This is <i>italic</i> text</p>";
        
        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);
        
        // Find the paragraph
        Assert.NotEmpty(layoutRoot.Children);
        var paragraph = layoutRoot.Children[0];
        
        // Find the italic element
        var italicElement = paragraph.Children.FirstOrDefault(c => c.HtmlNode?.Type == HtmlElementType.Italic);
        Assert.NotNull(italicElement);
        
        // Assert that the italic element has FontItalic = true
        Assert.True(italicElement.FontItalic, "Italic element should have FontItalic property set to true");
    }

    [Fact]
    public void MixedInlineElements_AllHaveCorrectStyling()
    {
        // Arrange
        var html = "<p>This is a <b>bold</b> and <i>italic</i> text demonstration.</p>";
        
        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);
        
        // Find the paragraph
        Assert.NotEmpty(layoutRoot.Children);
        var paragraph = layoutRoot.Children[0];
        
        // Find bold and italic elements
        var boldElement = paragraph.Children.FirstOrDefault(c => c.HtmlNode?.Type == HtmlElementType.Bold);
        var italicElement = paragraph.Children.FirstOrDefault(c => c.HtmlNode?.Type == HtmlElementType.Italic);
        
        Assert.NotNull(boldElement);
        Assert.NotNull(italicElement);
        
        Assert.True(boldElement.FontBold, "Bold element should have FontBold = true");
        Assert.True(italicElement.FontItalic, "Italic element should have FontItalic = true");
    }
}

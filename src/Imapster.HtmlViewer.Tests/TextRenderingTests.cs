using Imapster.HtmlViewer.Layout;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

public class TextRenderingTests
{
    private readonly LayoutEngine _layoutEngine;
    private readonly Parsing.HtmlParser _htmlParser;

    public TextRenderingTests()
    {
        _layoutEngine = new LayoutEngine();
        _htmlParser = new Parsing.HtmlParser();
    }

    [Fact]
    public void Layout_TextBetweenInlineElements_RendersCorrectly()
    {
        // This test reproduces the issue where plain text between inline elements
        // like <b>bold</b> plain text <i>italic</i> is not rendering properly
        var html = "<p>This is a <b>bold</b> and <i>italic</i> text demonstration.</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - The paragraph should have multiple children (text nodes + inline elements)
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];
        Assert.Equal(LayoutType.Block, paragraph.LayoutType);

        // Should have multiple children including text nodes and inline elements
        Assert.True(paragraph.Children.Count >= 3, "Paragraph should have at least 3 children (text + bold + text + italic + text)");

        // Check that all children have valid dimensions
        foreach (var child in paragraph.Children)
        {
            Assert.True(child.Width >= 0, $"Child {child.HtmlNode?.Type} should have valid width");
            Assert.True(child.Height >= 0, $"Child {child.HtmlNode?.Type} should have valid height");
        }
    }

    [Fact]
    public void Layout_SimpleTextBetweenInlineElements_RendersCorrectly()
    {
        // Simple test case: <p>before <b>bold</b> after</p>
        var html = "<p>before <b>bold</b> after</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Debug: Let's see what the HTML structure looks like
        Console.WriteLine("HTML Root Children Count: " + htmlRoot.Children.Count);
        foreach (var child in htmlRoot.Children)
        {
            Console.WriteLine($"  Child Type: {child.Type}, TextContent: '{child.TextContent}'");
            if (child.Children.Count > 0)
            {
                Console.WriteLine($"    Child Children Count: {child.Children.Count}");
                foreach (var grandChild in child.Children)
                {
                    Console.WriteLine($"      GrandChild Type: {grandChild.Type}, TextContent: '{grandChild.TextContent}'");
                }
            }
        }

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];
        Assert.Equal(LayoutType.Block, paragraph.LayoutType);

        // Should have at least 3 children: text, bold element, text
        Assert.True(paragraph.Children.Count >= 3, "Paragraph should have multiple children");

        // Check that all children have valid dimensions
        foreach (var child in paragraph.Children)
        {
            Assert.True(child.Width >= 0, $"Child {child.HtmlNode?.Type} should have valid width");
            Assert.True(child.Height >= 0, $"Child {child.HtmlNode?.Type} should have valid height");
        }
    }
}

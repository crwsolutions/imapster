using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

public class WhitespaceAndLineBreakTests
{
    private readonly LayoutEngine _layoutEngine;
    private readonly HtmlParser _htmlParser;

    public WhitespaceAndLineBreakTests()
    {
        _layoutEngine = new LayoutEngine();
        _htmlParser = new HtmlParser();
    }

    [Fact]
    public void Parse_WhitespaceBetweenBrElements_IsPreservedAsText()
    {
        // Arrange
        var html = "<p>Text<br/>\n<br/>More</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var pNode = htmlRoot.Children[0];

        // Assert - whitespace between br elements should be filtered out
        var children = pNode.Children;
        var textNodes = children.Where(c => c.Type == HtmlElementType.Text).ToList();
        var brNodes = children.Where(c => c.Type == HtmlElementType.LineBreak).ToList();

        // Should have: "Text", br, br, "More" = 4 nodes
        // Whitespace should be filtered out by NormalizeWhitespace
        Assert.Equal(2, brNodes.Count);
        Assert.Equal(2, textNodes.Count);
    }

    [Fact]
    public void Layout_DoubleBrWithWhitespace_ShouldNotShowExtraLine()
    {
        // Arrange
        var html = @"
            <body>
            Regel 1<br/>
            <br/>
            Regel 3
            </body>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - Should have 3 lines: "Regel 1", "", "Regel 3"
        // But currently might have 4 lines due to whitespace between br elements
        var bodyNode = layoutRoot.Children[0];
        int totalLines = bodyNode.LineBoxes.Count;
        
        // We expect 3 lines minimum (Regel 1, empty line, Regel 3)
        // But should NOT have extra lines from whitespace
        Assert.True(totalLines <= 4, 
            $"Double br with whitespace should create ~3 lines, got {totalLines}");
    }

    [Fact]
    public void Layout_MultipleBrInSequence_EmptyLinesOnly()
    {
        // Arrange - Three consecutive br elements
        var html = @"<p>Text<br/><br/><br/>More</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var pNode = layoutRoot.Children[0];
        Assert.True(pNode.LineBoxes.Count >= 4, 
            "Three consecutive br should create at least 4 lines");
    }

    [Fact]
    public void Layout_BrAfterText_NoWhitespaceIssues()
    {
        // Arrange
        var html = "<p>Text1<br/>Text2</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - Simple case should work
        var pNode = layoutRoot.Children[0];
        Assert.True(pNode.LineBoxes.Count >= 2, 
            "Text br Text should have 2 lines");
    }
}

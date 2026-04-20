using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

public class TextPositioningDebugTests
{
    private readonly LayoutEngine _layoutEngine;
    private readonly HtmlParser _htmlParser;

    public TextPositioningDebugTests()
    {
        _layoutEngine = new LayoutEngine();
        _htmlParser = new HtmlParser();
    }

    [Fact]
    public void Layout_TextAfterBr_TextNodesHaveLineBoxes()
    {
        // Arrange - Simple case with br between text
        var html = @"<body>Text1<br/>Text2</body>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);  // Returns DocumentFragment with text children
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - layoutRoot is the DocumentFragment node
        Assert.NotNull(layoutRoot);
        var documentNode = layoutRoot;  // This IS the DocumentFragment

        // The DocumentFragment should have text and br children
        Assert.NotEmpty(documentNode.Children);

        // Debug: Show what we have
        var allChildren = documentNode.Children;
        var textNodes = allChildren.Where(c => c.HtmlNode?.Type == HtmlElementType.Text).ToList();
        var brNodes = allChildren.Where(c => c.HtmlNode?.Type == HtmlElementType.LineBreak).ToList();

        Assert.True(textNodes.Count >= 2, $"Found {textNodes.Count} text nodes, expected >= 2");
        Assert.True(brNodes.Count >= 1, $"Found {brNodes.Count} br nodes, expected >= 1");

        // Check LineBoxes on text nodes
        foreach (var textNode in textNodes)
        {
            Assert.True(textNode.LineBoxes.Count > 0, 
                $"Text node should have LineBoxes. LayoutType={textNode.LayoutType}");
        }
    }

    [Fact]
    public void Layout_TextAfterBr_LinesHaveDifferentYPositions()
    {
        // Arrange
        var html = @"<body>Text1<br/>Text2</body>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);  // Returns DocumentFragment
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);
        var documentNode = layoutRoot;

        // Assert - The layout root should have multiple line boxes with different Y positions
        Assert.True(documentNode.LineBoxes.Count >= 2, 
            $"Should have at least 2 line boxes, but has {documentNode.LineBoxes.Count}");

        // Check that lines have different Y positions
        var firstLineY = documentNode.LineBoxes[0].Y;
        var secondLineY = documentNode.LineBoxes[1].Y;
        
        // Lines should have different Y positions (Text1 and Text2 should be on different lines)
        Assert.True(Math.Abs(firstLineY - secondLineY) > 0.001, 
            $"Text1 and Text2 should be on different lines. Y positions: {firstLineY} vs {secondLineY}");
    }
}

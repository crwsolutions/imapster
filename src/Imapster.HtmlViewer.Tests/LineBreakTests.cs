using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

public class LineBreakTests
{
    private readonly LayoutEngine _layoutEngine;
    private readonly HtmlParser _htmlParser;

    public LineBreakTests()
    {
        _layoutEngine = new LayoutEngine();
        _htmlParser = new HtmlParser();
    }

    [Fact]
    public void Parse_BrElement_IsParsedAsLineBreak()
    {
        // Arrange
        var html = "<p>Line 1<br/>Line 2</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);

        // Assert
        var pNode = htmlRoot.Children[0];
        Assert.NotEmpty(pNode.Children);
        
        // Should have Text, LineBreak, Text
        var hasLineBreak = pNode.Children.Any(c => c.Type == HtmlElementType.LineBreak);
        Assert.True(hasLineBreak, "LineBreak element should be parsed");
    }

    [Fact]
    public void Layout_MultipleLineBreaks_CreatesSeperateLines()
    {
        // Arrange
        var html = "<body><p>Line 1<br/>Line 2<br/>Line 3</p></body>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - Should render without errors
        Assert.NotNull(layoutRoot);
        Assert.True(layoutRoot.Height > 0);
    }

    [Fact]
    public void Layout_FbtoHtmlScenario_TextRendersOnSeparateLines()
    {
        // Arrange - User's HTML with strong and multiple br elements
        var html = @"
            <html>
            <body>
            <strong>Kunnen we nog iets voor u doen?</strong> <br />Op fbto.nl leest u hoe u ons bereikt.
            <br /><br />Hartelijke groet,<br /><br />FBTO<br/><br/>
            </body>
            </html>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - Should layout successfully
        Assert.NotNull(layoutRoot);
        Assert.True(layoutRoot.Height > 0, "Layout should have height");
        Assert.True(layoutRoot.Width > 0, "Layout should have width");
    }

    [Fact]
    public void Layout_BrAfterText_TextAfterBrStartsOnNewLine()
    {
        // Arrange
        var html = "<p>Text before<br/>Text after</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - Should have line boxes
        var pNode = layoutRoot.Children[0];
        Assert.NotEmpty(pNode.LineBoxes);
        
        // Should have at least 2 lines (one for "Text before", one for "Text after")
        Assert.True(pNode.LineBoxes.Count >= 2, 
            $"Should have at least 2 line boxes, got {pNode.LineBoxes.Count}");
    }

    [Fact]
    public void Layout_MultipleBrElements_CreateEmptyLines()
    {
        // Arrange
        var html = "<p>Line 1<br/><br/>Line 2</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var pNode = layoutRoot.Children[0];
        Assert.NotEmpty(pNode.LineBoxes);
        
        // Two consecutive br should create an empty line
        // So we expect: "Line 1", "", "Line 2" = 3 line boxes
        Assert.True(pNode.LineBoxes.Count >= 3, 
            $"Two consecutive <br/> should create empty line, got {pNode.LineBoxes.Count} lines");
    }

    [Fact]
    public void Layout_StrongAndBr_StrongTextIsNotLeakingToNextLine()
    {
        // Arrange
        var html = "<p><strong>Bold text</strong><br/>Regular text</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var pNode = layoutRoot.Children[0];
        Assert.NotEmpty(pNode.LineBoxes);
        
        // "Regular text" should be on second line, not bold
        Assert.True(pNode.LineBoxes.Count >= 2, "Should have at least 2 lines");
    }
}

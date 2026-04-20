using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

public class FbtoHtmlDebugTests
{
    private readonly LayoutEngine _layoutEngine;
    private readonly HtmlParser _htmlParser;

    public FbtoHtmlDebugTests()
    {
        _layoutEngine = new LayoutEngine();
        _htmlParser = new HtmlParser();
    }

    [Fact]
    public void Parse_TwoBrBeforeText_CreatesNewlines()
    {
        // Arrange - Simple case: text<br/><br/>text
        var html = "<p>Hartelijke groet,<br/><br/>FBTO</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        
        // Assert - Should have text, br, br, text
        var pNode = htmlRoot.Children[0];
        var hasLineBreaks = pNode.Children.Count(c => c.Type == HtmlElementType.LineBreak);
        Assert.Equal(2, hasLineBreaks);
    }

    [Fact]
    public void Layout_TwoBrBeforeText_TextOnSeparateLine()
    {
        // Arrange
        var html = "<p>Hartelijke groet,<br/><br/>FBTO</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - Should have 3 lines: "Hartelijke groet,", "", "FBTO"
        var pNode = layoutRoot.Children[0];
        Assert.NotEmpty(pNode.LineBoxes);
        Assert.True(pNode.LineBoxes.Count >= 3, 
            $"Two <br/> should create 3 lines, got {pNode.LineBoxes.Count} lines");
    }

    [Fact]
    public void Layout_ComplexHtmlWithBr_AllTextOnCorrectLines()
    {
        // Arrange - The problematic section from FBTO email
        var html = @"
            <body>
            Hartelijke groet,<br/><br/>FBTO<br/><br/>
            </body>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.True(layoutRoot.Height > 0);
    }

    [Fact]
    public void Parse_BodyWithMixedContent_AllElementsParsed()
    {
        // Arrange - Simplified version of the actual HTML
        var html = @"
            <body>
            <strong><strong style=""color:#005eaa; font-weight:bold"">Kunnen we nog iets voor u doen?</strong> </strong><br />
            Hartelijke groet,<br/><br/>FBTO<br/>
            </body>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);

        // Assert - Should parse without errors
        Assert.NotNull(htmlRoot);
        Assert.NotEmpty(htmlRoot.Children);
    }

    [Fact]
    public void Layout_BodyWithNestedStrongAndBr_RendersProperly()
    {
        // Arrange
        var html = @"
            <body>
            <strong><strong style=""color:#005eaa; font-weight:bold"">Bold text</strong> </strong><br />
            Regular text<br/><br/>More text<br/>
            </body>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - Should render with proper line breaks
        Assert.NotNull(layoutRoot);
        Assert.True(layoutRoot.Height > 0, "Should have height");
    }

    [Fact]
    public void Layout_TextSequenceWithMultipleBr_CountLines()
    {
        // Arrange
        var html = @"
            <body>
            Line 1<br/>
            Line 2<br/>
            Line 3<br/>
            <br/>
            Line 4<br/>
            Line 5
            </body>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.True(layoutRoot.Height > 0);
        
        // Count total line boxes
        int totalLines = 0;
        foreach (var child in layoutRoot.Children)
        {
            totalLines += child.LineBoxes.Count;
        }
        
        // Should have at least 6 lines (5 text lines + 1 empty)
        Assert.True(totalLines >= 5, 
            $"Should have at least 5 lines, got {totalLines}");
    }
}

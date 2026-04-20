using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

public class CenterElementTests
{
    private readonly LayoutEngine _layoutEngine;
    private readonly HtmlParser _htmlParser;

    public CenterElementTests()
    {
        _layoutEngine = new LayoutEngine();
        _htmlParser = new HtmlParser();
    }

    [Fact]
    public void Parse_CenterElement_IsParsedAsBlockElement()
    {
        // Arrange
        var html = "<center>Centered content</center>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);

        // Assert - Center element should be recognized
        Assert.NotNull(htmlRoot);
        Assert.Single(htmlRoot.Children);
        var centerNode = htmlRoot.Children[0];
        Assert.Equal(HtmlElementType.Center, centerNode.Type);
    }

    [Fact]
    public void Parse_CenterElement_AppliesTextAlignCenter()
    {
        // Arrange
        var html = "<center>Centered content</center>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);

        // Assert - Center element should have text-align: center styling
        Assert.NotNull(htmlRoot);
        Assert.Single(htmlRoot.Children);
        var centerNode = htmlRoot.Children[0];
        Assert.NotNull(centerNode.Style);
        Assert.Equal(Imapster.HtmlViewer.Parsing.TextAlignment.Center, centerNode.Style.TextAlign);
    }

    [Fact]
    public void Layout_CenterElement_HasBlockLayoutType()
    {
        // Arrange
        var html = "<center>Centered content</center>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - Center element should have Block layout type
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);
        var centerLayoutNode = layoutRoot.Children[0];
        Assert.Equal(LayoutType.Block, centerLayoutNode.LayoutType);
    }

    [Fact]
    public void Layout_CenterElementWithContent_HasNonZeroHeight()
    {
        // Arrange
        var html = "<center>Centered content</center>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - Center element should have non-zero height
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);
        var centerLayoutNode = layoutRoot.Children[0];
        Assert.True(centerLayoutNode.Height > 0, "Center element height should be greater than 0");
    }

    [Fact]
    public void Layout_CenterElementWithNestedElements_RendersProperly()
    {
        // Arrange
        var html = @"
            <center>
                <h1>Title</h1>
                <p>Paragraph text</p>
            </center>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - Center element should contain nested elements
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);
        var centerLayoutNode = layoutRoot.Children[0];
        Assert.Equal(HtmlElementType.Center, centerLayoutNode.HtmlNode.Type);
        Assert.True(centerLayoutNode.Children.Count > 0, "Center element should have child nodes");
        Assert.True(centerLayoutNode.Height > 0, "Center element should have non-zero height");
    }

    [Fact]
    public void Layout_CenterElementWithTable_CentersTableHorizontally()
    {
        // Arrange
        var html = @"
            <center>
                <table style=""width:200px;"">
                    <tr>
                        <td>Cell content</td>
                    </tr>
                </table>
            </center>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - Find the center and table nodes
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);
        var centerLayoutNode = layoutRoot.Children[0];
        Assert.Equal(HtmlElementType.Center, centerLayoutNode.HtmlNode.Type);

        // Center's child should be the table
        Assert.NotEmpty(centerLayoutNode.Children);
        var tableNode = centerLayoutNode.Children[0];
        Assert.Equal(HtmlElementType.Table, tableNode.HtmlNode.Type);

        // The table should be centered within the 800px width
        // With 200px width, it should be positioned around (800 - 200) / 2 = 300px from the left
        var expectedX = (800 - tableNode.Width) / 2;
        Assert.True(tableNode.X > 0, "Table X position should be greater than 0 (centered)");
        Assert.True(Math.Abs(tableNode.X - expectedX) < 1, $"Table should be centered, expected X ~{expectedX}, got {tableNode.X}");
    }

    [Fact]
    public void Layout_ComplexHtmlWithCenter_RendersProperly()
    {
        // Arrange - Using actual HTML from MainViewModel
        var html = @"
            <!DOCTYPE html>
            <html>
                <body>
                    <center>
                        <table>
                            <tr>
                                <td width=""700"">
                                    <div>Invoice Content</div>
                                </td>
                            </tr>
                        </table>
                    </center>
                </body>
            </html>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - The entire layout should render without errors
        Assert.NotNull(layoutRoot);
        Assert.True(layoutRoot.Height > 0, "Layout height should be greater than 0");
        Assert.True(layoutRoot.Width > 0, "Layout width should be greater than 0");
    }

    [Fact]
    public void Layout_TableInCenter_TablePositionIsCentered()
    {
        // Arrange - Create a simple table in a center element with known widths
        var html = @"
            <center>
                <table style=""width:300px;"">
                    <tr>
                        <td>Content</td>
                    </tr>
                </table>
            </center>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - Verify table is centered
        Assert.NotNull(layoutRoot);
        var centerNode = layoutRoot.Children[0];
        Assert.NotEmpty(centerNode.Children);
        var tableNode = centerNode.Children[0];

        // Table with 300px width in 800px container should start at (800-300)/2 = 250px
        var expectedMinX = 249; // Allow small rounding margin
        var expectedMaxX = 251;
        Assert.True(tableNode.X >= expectedMinX && tableNode.X <= expectedMaxX, 
            $"Table X position should be around 250, got {tableNode.X}");
    }
}

using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

/// <summary>
/// Tests for display:none handling (e.g. email preheader hacks) and legacy table width
/// attributes (e.g. the classic &lt;td align="center"&gt;&lt;table width="640"&gt; email layout).
/// </summary>
public class DisplayNoneAndTableWidthTests
{
    private readonly LayoutEngine _layoutEngine;
    private readonly HtmlParser _htmlParser;

    public DisplayNoneAndTableWidthTests()
    {
        _layoutEngine = new LayoutEngine();
        _htmlParser = new HtmlParser();
    }

    #region Display none tests

    [Fact]
    public void Layout_DisplayNoneDiv_IsNotRendered()
    {
        // Arrange - the Consumentenbond preheader hack: a hidden div full of U+0357 characters
        var html = @"<div style=""display: none; width: 0; height: 0; max-height: 0; overflow: hidden"">&#847; &#847; &#847; &#847;</div>"
                   + "<p>Visible content</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - only the paragraph is laid out
        var rendered = Flatten(layoutRoot).ToList();
        Assert.DoesNotContain(rendered, n => n.HtmlNode?.TextContent?.Contains('\u0357') == true);
        Assert.Contains(rendered, n => n.HtmlNode?.TextContent == "Visible content");
    }

    [Fact]
    public void Layout_DisplayNoneWithImportant_IsNotRendered()
    {
        // Arrange
        var html = @"<div class=""preheader"" style=""font-size: 1px; display: none !important;"">Hidden preheader</div><p>Visible</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var text = Flatten(layoutRoot).Select(n => n.HtmlNode?.TextContent ?? string.Empty).Aggregate(string.Empty, (a, b) => a + b);
        Assert.DoesNotContain("Hidden preheader", text);
        Assert.Contains("Visible", text);
    }

    [Fact]
    public void Layout_DisplayInline_IsRendered()
    {
        // Arrange - display: inline is not display: none and must remain visible
        var html = @"<div style=""display: inline; width: 0; height: 0;"">Content</div>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var text = Flatten(layoutRoot).Select(n => n.HtmlNode?.TextContent ?? string.Empty).Aggregate(string.Empty, (a, b) => a + b);
        Assert.Contains("Content", text);
    }

    [Fact]
    public void Layout_DisplayNoneTable_IsNotRendered()
    {
        // Arrange - a hidden table (some emails hide desktop-only sections)
        var html = @"<table style=""display:none;""><tr><td>Hidden cell</td></tr></table><p>Visible</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var text = Flatten(layoutRoot).Select(n => n.HtmlNode?.TextContent ?? string.Empty).Aggregate(string.Empty, (a, b) => a + b);
        Assert.DoesNotContain("Hidden cell", text);
        Assert.Contains("Visible", text);
    }

    #endregion

    #region Legacy table width attribute tests

    [Fact]
    public void Layout_TableWithWidthAttribute_HasFixedWidth()
    {
        // Arrange
        var html = @"<table width=""640""><tr><td>Content</td></tr></table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var table = layoutRoot.Children[0];
        Assert.True(table.WidthSet, "Table with width attribute should have a set width");
        Assert.Equal(640, table.Width, precision: 2);
    }

    [Fact]
    public void Layout_TableWithWidthAttribute_IsCenteredInParent()
    {
        // Arrange - the classic email wrapper: center-aligned cell containing a fixed table
        var html = @"<table width=""100%""><tr><td align=""center"">
            <table width=""640""><tr><td>Content</td></tr></table>
        </td></tr></table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var outer = layoutRoot.Children[0];
        var outerTbody = outer.Children[0];
        var outerRow = outerTbody.Children[0];
        var outerCell = outerRow.Children[0];

        var innerTable = outerCell.Children[0];
        Assert.Equal(640, innerTable.Width, precision: 2);
        // Centered in the 800px cell: (800 - 640) / 2 = 80
        Assert.Equal(80, innerTable.X, precision: 2);
    }

    [Fact]
    public void Layout_TableWithWidthGreaterThanAvailable_IsClamped()
    {
        // Arrange
        var html = @"<table width=""900""><tr><td>Content</td></tr></table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var table = layoutRoot.Children[0];
        Assert.Equal(800, table.Width, precision: 2);
    }

    [Fact]
    public void Layout_TableWithPercentageWidth_IsResolved()
    {
        // Arrange
        var html = @"<table width=""50%""><tr><td>Content</td></tr></table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var table = layoutRoot.Children[0];
        Assert.Equal(400, table.Width, precision: 2);
    }

    [Fact]
    public void Layout_TableWithoutWidthAttribute_UsesAvailableWidth()
    {
        // Arrange
        var html = @"<table><tr><td>Content</td></tr></table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var table = layoutRoot.Children[0];
        Assert.False(table.WidthSet);
        Assert.Equal(800, table.Width, precision: 2);
    }

    [Fact]
    public void Layout_CellsWithWidthAttributes_GetExplicitWidths()
    {
        // Arrange - the Consumentenbond body row: 20px spacer, content, 20px spacer
        var html = @"<table width=""100%""><tr>
            <td width=""20"">&nbsp;</td>
            <td>Content</td>
            <td width=""20"">&nbsp;</td>
        </tr></table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var table = layoutRoot.Children[0];
        var row = table.Children[0].Children[0];
        var cells = row.Children;

        Assert.Equal(20, cells[0].Width, precision: 2);
        Assert.Equal(760, cells[1].Width, precision: 2);
        Assert.Equal(20, cells[2].Width, precision: 2);
        Assert.Equal(0, cells[0].X, precision: 2);
        Assert.Equal(20, cells[1].X, precision: 2);
        Assert.Equal(780, cells[2].X, precision: 2);
    }

    [Fact]
    public void Layout_FixedCellsExceedingAvailableWidth_AreScaledDown()
    {
        // Arrange - two fixed cells of 500px in a 400px row
        var html = @"<table width=""400""><tr>
            <td width=""500"">A</td>
            <td width=""500"">B</td>
        </tr></table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - table is 400px, cells scaled to 200px each
        var table = layoutRoot.Children[0];
        var row = table.Children[0].Children[0];
        var cells = row.Children;

        Assert.Equal(200, cells[0].Width, precision: 2);
        Assert.Equal(200, cells[1].Width, precision: 2);
        Assert.Equal(200, cells[1].X, precision: 2);
    }

    [Fact]
    public void Layout_ColspanStillWorks_WithoutCellWidths()
    {
        // Arrange - cell with colspan=2 should take 2/3 of the row
        var html = @"<table width=""300""><tr>
            <td width=""100"">A</td>
            <td colspan=""2"">B</td>
            <td>C</td>
        </tr></table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - table is 300px wide; A=100, B (flexible, colspan 2) + C (flexible, 1) share 200
        var table = layoutRoot.Children[0];
        var row = table.Children[0].Children[0];
        var cells = row.Children;

        Assert.Equal(100, cells[0].Width, precision: 2);
        Assert.Equal(133.33, cells[1].Width, precision: 1);
        Assert.Equal(66.67, cells[2].Width, precision: 1);
    }

    #endregion

    /// <summary>
    /// Flattens the layout tree, returning every node.
    /// </summary>
    private IEnumerable<LayoutNode> Flatten(LayoutNode node)
    {
        yield return node;

        foreach (var child in node.Children)
        {
            foreach (var descendant in Flatten(child))
                yield return descendant;
        }
    }
}

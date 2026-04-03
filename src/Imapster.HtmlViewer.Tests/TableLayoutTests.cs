using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

/// <summary>
/// Tests for table layout functionality.
/// These tests verify that tables are properly parsed and laid out.
/// </summary>
public class TableLayoutTests
{
    private readonly LayoutEngine _layoutEngine;
    private readonly HtmlParser _htmlParser;

    public TableLayoutTests()
    {
        _layoutEngine = new LayoutEngine();
        _htmlParser = new HtmlParser();
    }

    [Fact]
    public void Layout_SimpleTable_HasCorrectStructure()
    {
        // Arrange
        var html = @"
            <table>
                <tr>
                    <th>Header 1</th>
                    <th>Header 2</th>
                </tr>
                <tr>
                    <td>Cell 1</td>
                    <td>Cell 2</td>
                </tr>
            </table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - DocumentFragment root contains the table as its child
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var table = layoutRoot.Children[0];
        Assert.Equal(LayoutType.Block, table.LayoutType);
        Assert.True(table.Height > 0, "Table should have positive height");
        Assert.True(table.Width > 0, "Table should have positive width");
    }

    [Fact]
    public void Layout_Table_HasTableRowChildren()
    {
        // Arrange
        var html = @"
            <table>
                <tr>
                    <td>Row 1 Cell 1</td>
                </tr>
                <tr>
                    <td>Row 2 Cell 1</td>
                </tr>
            </table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var table = layoutRoot.Children[0];

        // Table should have a tbody child (AngleSharp wraps rows in tbody)
        Assert.Single(table.Children);
        Assert.Equal(HtmlElementType.TableBody, table.Children[0].HtmlNode?.Type);

        var tbody = table.Children[0];

        // tbody should have row children
        Assert.True(tbody.Children.Count >= 2, "tbody should have at least 2 row children");

        // Each child should be a table row
        foreach (var row in tbody.Children)
        {
            Assert.Equal(HtmlElementType.TableRow, row.HtmlNode?.Type);
            Assert.Equal(LayoutType.Block, row.LayoutType);
        }
    }

    [Fact]
    public void Layout_TableRow_HasTableCellChildren()
    {
        // Arrange
        var html = @"
            <table>
                <tr>
                    <td>Cell 1</td>
                    <td>Cell 2</td>
                    <td>Cell 3</td>
                </tr>
            </table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var table = layoutRoot.Children[0];
        Assert.Single(table.Children); // One tbody

        var tbody = table.Children[0];
        Assert.Equal(HtmlElementType.TableBody, tbody.HtmlNode?.Type);
        Assert.Single(tbody.Children); // One row

        var row = tbody.Children[0];
        Assert.Equal(HtmlElementType.TableRow, row.HtmlNode?.Type);

        // Row should have 3 cell children
        Assert.Equal(3, row.Children.Count);

        // Each child should be a table cell
        foreach (var cell in row.Children)
        {
            Assert.Equal(HtmlElementType.TableCell, cell.HtmlNode?.Type);
        }
    }

    [Fact]
    public void Layout_TableHeaderCell_HasCorrectType()
    {
        // Arrange
        var html = @"
            <table>
                <tr>
                    <th>Header</th>
                </tr>
            </table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var table = layoutRoot.Children[0];
        Assert.Single(table.Children); // One tbody

        var tbody = table.Children[0];
        Assert.Single(tbody.Children); // One row

        var row = tbody.Children[0];
        Assert.Single(row.Children); // One cell

        var cell = row.Children[0];
        Assert.Equal(HtmlElementType.TableHeaderCell, cell.HtmlNode?.Type);
    }

    [Fact]
    public void Layout_TableCells_HavePositiveWidth()
    {
        // Arrange
        var html = @"
            <table>
                <tr>
                    <td>Cell with text</td>
                    <td>Another cell</td>
                </tr>
            </table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var table = layoutRoot.Children[0];
        Assert.Single(table.Children); // One tbody

        var tbody = table.Children[0];
        Assert.Single(tbody.Children); // One row

        var row = tbody.Children[0];

        // Each cell should have positive width
        foreach (var cell in row.Children)
        {
            Assert.True(cell.Width > 0, 
                $"Cell '{cell.HtmlNode?.TextContent}' should have positive width");
        }
    }

    [Fact]
    public void Layout_TableCells_HavePositiveHeight()
    {
        // Arrange
        var html = @"
            <table>
                <tr>
                    <td>Cell with text</td>
                </tr>
            </table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var table = layoutRoot.Children[0];
        Assert.Single(table.Children); // One tbody

        var tbody = table.Children[0];
        Assert.Single(tbody.Children); // One row

        var row = tbody.Children[0];
        Assert.Single(row.Children); // One cell

        var cell = row.Children[0];
        Assert.True(cell.Height > 0, "Cell should have positive height");
    }

    [Fact]
    public void Layout_TableRows_AreStackedVertically()
    {
        // Arrange
        var html = @"
            <table>
                <tr><td>Row 1</td></tr>
                <tr><td>Row 2</td></tr>
                <tr><td>Row 3</td></tr>
            </table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var table = layoutRoot.Children[0];
        Assert.Single(table.Children); // One tbody

        var tbody = table.Children[0];
        Assert.Equal(3, tbody.Children.Count);

        // Rows should be stacked vertically (increasing Y positions)
        for (int i = 1; i < tbody.Children.Count; i++)
        {
            var prevRow = tbody.Children[i - 1];
            var currRow = tbody.Children[i];

            Assert.True(currRow.Y >= prevRow.Y, 
                $"Row {i} should be at or below row {i - 1}");
        }
    }

    [Fact]
    public void Layout_TableCells_ArePositionedHorizontally()
    {
        // Arrange
        var html = @"
            <table>
                <tr>
                    <td>Cell 1</td>
                    <td>Cell 2</td>
                    <td>Cell 3</td>
                </tr>
            </table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var table = layoutRoot.Children[0];
        Assert.Single(table.Children); // One tbody

        var tbody = table.Children[0];
        Assert.Single(tbody.Children); // One row

        var row = tbody.Children[0];
        Assert.Equal(3, row.Children.Count);

        // Cells should be positioned horizontally (increasing X positions)
        for (int i = 1; i < row.Children.Count; i++)
        {
            var prevCell = row.Children[i - 1];
            var currCell = row.Children[i];

            Assert.True(currCell.X >= prevCell.X, 
                $"Cell {i} should be at or to the right of cell {i - 1}");
        }
    }

    [Fact]
    public void Layout_ComplexTable_HasCorrectStructure()
    {
        // Arrange
        var html = @"
            <table>
                <tr>
                    <th>Name</th>
                    <th>Age</th>
                    <th>City</th>
                </tr>
                <tr>
                    <td>John</td>
                    <td>30</td>
                    <td>New York</td>
                </tr>
                <tr>
                    <td>Jane</td>
                    <td>25</td>
                    <td>London</td>
                </tr>
            </table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var table = layoutRoot.Children[0];
        Assert.Single(table.Children); // One tbody (AngleSharp wraps all rows in tbody)

        var tbody = table.Children[0];
        Assert.Equal(3, tbody.Children.Count);

        // First row should have header cells
        var headerRow = tbody.Children[0];
        Assert.Equal(3, headerRow.Children.Count);
        Assert.All(headerRow.Children, cell => 
            Assert.Equal(HtmlElementType.TableHeaderCell, cell.HtmlNode?.Type));

        // Second and third rows should have regular cells
        var dataRow1 = tbody.Children[1];
        Assert.Equal(3, dataRow1.Children.Count);
        Assert.All(dataRow1.Children, cell => 
            Assert.Equal(HtmlElementType.TableCell, cell.HtmlNode?.Type));

        var dataRow2 = tbody.Children[2];
        Assert.Equal(3, dataRow2.Children.Count);
        Assert.All(dataRow2.Children, cell => 
            Assert.Equal(HtmlElementType.TableCell, cell.HtmlNode?.Type));
    }
}
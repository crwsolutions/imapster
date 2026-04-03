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

    #region Text Wrapping Tests

    [Fact]
    public void Layout_TableCellWithLongTextAndInlineFormatting_WrapsCorrectly()
    {
        // Arrange - lange tekst met inline opmaak in een tabelcel
        var html = @"
            <table>
                <tr>
                    <td>This paragraph demonstrates <b>bold text</b>, <strong>strong text</strong>, 
                    <i>italic text</i>, <em>emphasized text</em>, <u>underlined text</u>, 
                    <mark style='background-color: #FFFF00;'>highlighted text</mark>, 
                    <small>small text</small>, <del>deleted text</del>, and <ins>inserted text</ins>.</td>
                </tr>
            </table>";

        // Act - gebruik smalle breedte om wrapping te forceren
        var htmlRoot = _htmlParser.Parse(html);
        var narrowLayoutRoot = _layoutEngine.Layout(htmlRoot, 300); // Zeer smal
        var wideLayoutRoot = _layoutEngine.Layout(htmlRoot, 800);   // Breed - geen wrapping

        // Debug: Log de werkelijke waarden
        var narrowTable = narrowLayoutRoot.Children[0];
        var wideTable = wideLayoutRoot.Children[0];
        var narrowTbody = narrowTable.Children[0];
        var wideTbody = wideTable.Children[0];
        var narrowRow = narrowTbody.Children[0];
        var wideRow = wideTbody.Children[0];
        var narrowCell = narrowRow.Children[0];
        var wideCell = wideRow.Children[0];

        // Debug output
        System.Diagnostics.Debug.WriteLine($"Narrow cell: Height={narrowCell.Height}, Width={narrowCell.Width}, LineBoxes.Count={narrowCell.LineBoxes?.Count ?? 0}");
        System.Diagnostics.Debug.WriteLine($"Wide cell: Height={wideCell.Height}, Width={wideCell.Width}, LineBoxes.Count={wideCell.LineBoxes?.Count ?? 0}");

        // Assert - check if wrapping is happening by looking at LineBoxes
        Assert.NotNull(narrowCell.LineBoxes);
        Assert.NotNull(wideCell.LineBoxes);

        // Narrow cell should have more lines than wide cell
        Assert.True(narrowCell.LineBoxes.Count > wideCell.LineBoxes.Count,
            $"Narrow cell should have more lines ({narrowCell.LineBoxes.Count}) than wide cell ({wideCell.LineBoxes.Count}) due to wrapping");
    }

    [Fact]
    public void Layout_TableCellWithLongPlainText_WrapsCorrectly()
    {
        // Arrange - lange plain text zonder inline opmaak in een tabelcel
        var html = @"
            <table>
                <tr>
                    <td>This is a verylongwordthatshouldcauseoverflow plain text without any inline formatting that should wrap to multiple lines in a narrow table cell.</td>
                </tr>
            </table>";

        // Act - gebruik smalle breedte om wrapping te forceren
        var htmlRoot = _htmlParser.Parse(html);
        var narrowLayoutRoot = _layoutEngine.Layout(htmlRoot, 150); // Zeer smal
        var wideLayoutRoot = _layoutEngine.Layout(htmlRoot, 800);   // Breed - geen wrapping

        // Debug: Log de werkelijke waarden
        var narrowTable = narrowLayoutRoot.Children[0];
        var wideTable = wideLayoutRoot.Children[0];
        var narrowTbody = narrowTable.Children[0];
        var wideTbody = wideTable.Children[0];
        var narrowRow = narrowTbody.Children[0];
        var wideRow = wideTbody.Children[0];
        var narrowCell = narrowRow.Children[0];
        var wideCell = wideRow.Children[0];

        // Debug output
        System.Diagnostics.Debug.WriteLine($"Narrow cell: Height={narrowCell.Height}, Width={narrowCell.Width}, LineBoxes.Count={narrowCell.LineBoxes?.Count ?? 0}");
        System.Diagnostics.Debug.WriteLine($"Wide cell: Height={wideCell.Height}, Width={wideCell.Width}, LineBoxes.Count={wideCell.LineBoxes?.Count ?? 0}");

        // Assert - check if wrapping is happening by looking at LineBoxes
        Assert.NotNull(narrowCell.LineBoxes);
        Assert.NotNull(wideCell.LineBoxes);

        // Narrow cell should have more lines than wide cell
        Assert.True(narrowCell.LineBoxes.Count > wideCell.LineBoxes.Count,
            $"Narrow cell should have more lines ({narrowCell.LineBoxes.Count}) than wide cell ({wideCell.LineBoxes.Count}) due to wrapping");
    }

    #endregion

    #region LineBreak Tests

    [Fact]
    public void Layout_TableCellWithLineBreak_SplitsIntoMultipleLines()
    {
        // Arrange - tekst met <br> elementen in een tabelcel
        var html = @"
            <table>
                <tr>
                    <td>First line<br>Second line<br>Third line</td>
                </tr>
            </table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 300);

        // Assert
        var table = layoutRoot.Children[0];
        var tbody = table.Children[0];
        var row = tbody.Children[0];
        var cell = row.Children[0];

        Assert.NotNull(cell.LineBoxes);
        // Should have 3 lines due to 2 <br> elements
        Assert.Equal(3, cell.LineBoxes.Count);
    }

    [Fact]
    public void Layout_TableCellWithLineBreakAndLongText_WrapsCorrectly()
    {
        // Arrange - lange tekst met <br> elementen in een smalle tabelcel
        var html = @"
            <table>
                <tr>
                    <td>This is a very long first line that should wrap<br>Second line with <b>bold text</b> that should also wrap<br>Third line with <i>italic text</i></td>
                </tr>
            </table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 200);

        // Assert
        var table = layoutRoot.Children[0];
        var tbody = table.Children[0];
        var row = tbody.Children[0];
        var cell = row.Children[0];

        Assert.NotNull(cell.LineBoxes);
        // Should have multiple lines due to <br> elements and wrapping
        Assert.True(cell.LineBoxes.Count >= 3,
            $"Expected at least 3 lines, got {cell.LineBoxes.Count}");
    }

    #endregion

    #region NonBreakingSpace Tests

    [Fact]
    public void Layout_TableCellWithOnlyNonBreakingSpace_HasPositiveHeight()
    {
        // Arrange - cel met alleen &nbsp;
        var html = @"
            <table>
                <tr>
                    <td>&nbsp;</td>
                </tr>
            </table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 300);

        // Assert
        var table = layoutRoot.Children[0];
        var tbody = table.Children[0];
        var row = tbody.Children[0];
        var cell = row.Children[0];

        // Cell with &nbsp; should have positive height (at least one line)
        Assert.True(cell.Height > 0,
            $"Cell with &nbsp; should have positive height, got {cell.Height}");
    }

    [Fact]
    public void Layout_TableCellWithTextAndNonBreakingSpace_HasCorrectHeight()
    {
        // Arrange - cel met tekst en &nbsp;
        var html = @"
            <table>
                <tr>
                    <td>Some text&nbsp;and more text</td>
                </tr>
            </table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 300);

        // Assert
        var table = layoutRoot.Children[0];
        var tbody = table.Children[0];
        var row = tbody.Children[0];
        var cell = row.Children[0];

        // Cell should have positive height
        Assert.True(cell.Height > 0,
            $"Cell with text and &nbsp; should have positive height, got {cell.Height}");
    }

    #endregion
}

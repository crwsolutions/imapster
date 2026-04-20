using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

public class AlignAttributeTests
{
    private readonly LayoutEngine _layoutEngine;
    private readonly HtmlParser _htmlParser;

    public AlignAttributeTests()
    {
        _layoutEngine = new LayoutEngine();
        _htmlParser = new HtmlParser();
    }

    [Fact]
    public void Parse_TdWithAlignRight_AppliesTextAlignRight()
    {
        // Arrange
        var html = "<table><tr><td align=\"right\">Right aligned content</td></tr></table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);

        // Assert - Find the td element and verify alignment
        var tableNode = htmlRoot.Children[0];
        var tbodyNode = tableNode.Children[0];
        var trNode = tbodyNode.Children[0];
        var tdNode = trNode.Children[0];

        Assert.NotNull(tdNode.Style);
        Assert.Equal(Imapster.HtmlViewer.Parsing.TextAlignment.Right, tdNode.Style.TextAlign);
    }

    [Fact]
    public void Parse_TdWithAlignCenter_AppliesTextAlignCenter()
    {
        // Arrange
        var html = "<table><tr><td align=\"center\">Centered content</td></tr></table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);

        // Assert
        var tableNode = htmlRoot.Children[0];
        var tbodyNode = tableNode.Children[0];
        var trNode = tbodyNode.Children[0];
        var tdNode = trNode.Children[0];

        Assert.NotNull(tdNode.Style);
        Assert.Equal(Imapster.HtmlViewer.Parsing.TextAlignment.Center, tdNode.Style.TextAlign);
    }

    [Fact]
    public void Parse_DivWithAlignRight_AppliesTextAlignRight()
    {
        // Arrange
        var html = "<div align=\"right\">Right aligned div</div>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);

        // Assert
        var divNode = htmlRoot.Children[0];
        Assert.NotNull(divNode.Style);
        Assert.Equal(Imapster.HtmlViewer.Parsing.TextAlignment.Right, divNode.Style.TextAlign);
    }

    [Fact]
    public void Layout_TableCellWithAlignRight_CellContentIsRightAligned()
    {
        // Arrange
        var html = @"
            <table>
                <tr>
                    <td align=""right"" width=""200"">Right aligned</td>
                </tr>
            </table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - Find the td layout node and verify it has right text alignment
        var tableNode = layoutRoot.Children[0];
        var tbodyNode = tableNode.Children[0];
        var trNode = tbodyNode.Children[0];
        var tdNode = trNode.Children[0];

        Assert.Equal(Imapster.HtmlViewer.Parsing.TextAlignment.Right, tdNode.TextAlign);
    }

    [Fact]
    public void Parse_StyleAttributeTakesPrecedenceOverAlign()
    {
        // Arrange - explicit style attribute should take precedence over align attribute
        var html = "<td align=\"right\" style=\"text-align: left;\">Content</td>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);

        // Assert - style attribute is parsed first, then align is applied, but since
        // align=right would override left, we need to check order
        // Actually, ParseInlineStyles is called first, then ParseDeprecatedAttributes
        // So deprecated attributes should NOT override explicit styles
        var tdNode = htmlRoot.Children[0];
        Assert.NotNull(tdNode.Style);
        // After fix: style attribute (left) should be preserved
        Assert.Equal(Imapster.HtmlViewer.Parsing.TextAlignment.Left, tdNode.Style.TextAlign);
    }

    [Fact]
    public void Parse_InvalidAlignValue_KeepsExistingAlignment()
    {
        // Arrange
        var html = "<td align=\"invalid\">Content</td>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);

        // Assert - Invalid align value should not change the default (Left)
        var tdNode = htmlRoot.Children[0];
        Assert.NotNull(tdNode.Style);
        Assert.Equal(Imapster.HtmlViewer.Parsing.TextAlignment.Left, tdNode.Style.TextAlign);
    }

    [Fact]
    public void Parse_TdWithAlignJustify_AppliesTextAlignJustify()
    {
        // Arrange
        var html = "<table><tr><td align=\"justify\">Justified content</td></tr></table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);

        // Assert
        var tableNode = htmlRoot.Children[0];
        var tbodyNode = tableNode.Children[0];
        var trNode = tbodyNode.Children[0];
        var tdNode = trNode.Children[0];

        Assert.NotNull(tdNode.Style);
        Assert.Equal(Imapster.HtmlViewer.Parsing.TextAlignment.Justify, tdNode.Style.TextAlign);
    }

    [Fact]
    public void Layout_ComplexTableWithAlignAttributes_RendersCorrectly()
    {
        // Arrange - A realistic table with various align attributes
        var html = @"
            <table>
                <tr>
                    <td align=""left"">Left</td>
                    <td align=""center"">Center</td>
                    <td align=""right"">Right</td>
                </tr>
            </table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - Verify the layout renders without errors
        Assert.NotNull(layoutRoot);
        Assert.True(layoutRoot.Height > 0);
        Assert.True(layoutRoot.Width > 0);
    }
}

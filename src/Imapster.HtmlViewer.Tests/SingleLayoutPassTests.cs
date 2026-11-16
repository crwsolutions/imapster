using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

/// <summary>
/// A node is laid out once for the width its containing block resolves for it. Marketing emails
/// nest fixed-width tables several levels deep, and when a containing block laid a subtree out
/// again at its own width, both the work and the number of produced line boxes grew with the
/// nesting depth instead of with the content.
///
/// Line boxes on inline children are not duplicates of the drawing pass: the renderer uses the
/// block's line boxes for positioning and the inline child's line boxes for styling, so these
/// tests compare counts between shapes of the same content rather than counting a fixed number.
/// </summary>
public class SingleLayoutPassTests
{
    private readonly LayoutEngine _layoutEngine = new();
    private readonly HtmlParser _htmlParser = new();

    [Fact]
    public void Layout_NestedTables_LineBoxCountDoesNotGrowWithDepth()
    {
        // Act - the same single line of text, wrapped in one table and in six
        var shallow = _layoutEngine.Layout(_htmlParser.Parse(Nested(1)), 800);
        var deep = _layoutEngine.Layout(_htmlParser.Parse(Nested(6)), 800);

        // Assert - nesting adds nodes, not layout work
        Assert.Equal(shallow.Height, deep.Height);
        Assert.Equal(CountLineBoxes(shallow), CountLineBoxes(deep));
    }

    [Fact]
    public void Layout_RepeatedRows_LineBoxCountGrowsLinearlyWithContent()
    {
        // Act
        var tenRows = _layoutEngine.Layout(_htmlParser.Parse(Rows(10)), 800);
        var twentyRows = _layoutEngine.Layout(_htmlParser.Parse(Rows(20)), 800);

        // Assert - doubling the content doubles the line boxes, it does not square them
        Assert.Equal(CountLineBoxes(tenRows) * 2, CountLineBoxes(twentyRows));
        Assert.Equal(tenRows.Height * 2, twentyRows.Height);

        // Assert - two short paragraphs per row produce a handful of line boxes, not one copy
        // per enclosing element
        var perRow = CountLineBoxes(twentyRows) / 20;
        Assert.True(perRow <= 10, $"each row holds two one-line paragraphs but produces {perRow} line boxes");
    }

    [Fact]
    public void Layout_ContentWrappedInOneMoreTable_KeepsLineBoxCount()
    {
        // Arrange - the same rows, wrapped in one extra table level
        var rows = Rows(10);
        var wrapped = $"<table width=\"600\"><tbody><tr><td width=\"600\">{rows}</td></tr></tbody></table>";

        // Act
        var flat = _layoutEngine.Layout(_htmlParser.Parse(rows), 800);
        var wrappedLayout = _layoutEngine.Layout(_htmlParser.Parse(wrapped), 800);

        // Assert - a wrapper table is one row of nodes, not another copy of every line inside it
        Assert.Equal(flat.Height, wrappedLayout.Height);
        Assert.Equal(CountLineBoxes(flat), CountLineBoxes(wrappedLayout));
    }

    [Fact]
    public void Layout_NestedTables_KeepWidthWithinContainingBlock()
    {
        // Arrange - an inner table wider than the cell that contains it
        var html = """
                   <table width="300"><tbody><tr><td width="300">
                     <table width="600"><tbody><tr><td width="600">
                       <p>Narrow cell content</p>
                     </td></tr></tbody></table>
                   </td></tr></tbody></table>
                   """;

        // Act
        var layoutRoot = _layoutEngine.Layout(_htmlParser.Parse(html), 250);

        // Assert - nothing overflows the available width, so no horizontal scrolling appears
        var widest = WidestRightEdge(layoutRoot, 0);
        Assert.True(widest <= 250.5, $"layout reaches x={widest} while only 250 is available");
    }

    private static string Nested(int depth)
    {
        var open = string.Concat(Enumerable.Repeat("<table width=\"600\"><tbody><tr><td width=\"600\">", depth));
        var close = string.Concat(Enumerable.Repeat("</td></tr></tbody></table>", depth));
        return $"<div>{open}<p><span>Order summary line</span></p>{close}</div>";
    }

    private static string Rows(int rows)
    {
        var body = string.Concat(Enumerable.Range(1, rows).Select(i =>
            "<table width=\"600\"><tbody><tr>" +
            $"<td width=\"300\"><p>Label {i}</p></td><td width=\"300\"><p><strong>Value {i}</strong></p></td>" +
            "</tr></tbody></table>"));
        return $"<div>{body}</div>";
    }

    private static int CountLineBoxes(LayoutNode node)
    {
        var count = node.LineBoxes.Count;
        foreach (var child in node.Children)
            count += CountLineBoxes(child);
        return count;
    }

    /// <summary>
    /// Node offsets are relative to their parent, so the right edge is accumulated while walking.
    /// </summary>
    private static double WidestRightEdge(LayoutNode node, double parentX)
    {
        var x = parentX + node.X;
        var widest = x + node.Width;

        foreach (var child in node.Children)
            widest = Math.Max(widest, WidestRightEdge(child, x));

        return widest;
    }
}

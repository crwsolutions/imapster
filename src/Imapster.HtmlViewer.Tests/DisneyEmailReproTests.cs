using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

/// <summary>
/// Regression tests for the "!important" handling of inline styles. Marketing emails
/// (e.g. the Disney+ price-increase email) set <c>style="width: 480px !important"</c>
/// on their fixed-width wrapper table. The "!important" modifier used to be passed to the
/// length parser, which could not parse it and returned 0 while still flagging the width
/// as set - collapsing the table (and every nested cell) to a width of 0 and producing
/// one word per line of body text.
/// </summary>
public class DisneyEmailReproTests
{
    private readonly LayoutEngine _layoutEngine;
    private readonly HtmlParser _htmlParser;

    public DisneyEmailReproTests()
    {
        _layoutEngine = new LayoutEngine();
        _htmlParser = new HtmlParser();
    }

    // Mirrors the structure of the Disney+ email:
    //   <table width="100%">                                        (body)
    //     <table width="480" style="width: 480px !important">       (fixed wrapper)
    //       <td align="center">
    //         <table width="100%">
    //           <td><table class="module_1"><td>... body text ...
    private const string DisneyStructureHtml = @"
<table width=""100%"">
  <tr>
    <td>
      <table width=""480"" style=""width: 480px !important; bgcolor:#02172A"">
        <tr>
          <td align=""center"">
            <table width=""100%"">
              <tr>
                <td>
                  <table class=""module_1"" width=""100%"" style=""margin-bottom:-1px"">
                    <tr>
                      <td align=""left"" style=""padding:48px 40px 24px; font-size:20px; color:#000000; line-height:26px"">
                        Hallo, Bedankt dat je lid van de Disney+-community bent. We vinden het geweldig dat je geniet van ons aanbod: van blockbusters en gloednieuwe series tot exclusieve Originals.
                      </td>
                    </tr>
                  </table>
                </td>
              </tr>
            </table>
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>";

    private static LayoutNode? FindLayoutNode(LayoutNode node, Func<LayoutNode, bool> predicate)
    {
        if (predicate(node))
            return node;

        foreach (var child in node.Children)
        {
            var match = FindLayoutNode(child, predicate);
            if (match is not null)
                return match;
        }

        return null;
    }

    [Fact]
    public void DisneyEmail_InlineWidthImportant_KeepsTableWidth()
    {
        // Act
        var htmlRoot = _htmlParser.Parse(DisneyStructureHtml);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // The fixed wrapper table (the one carrying both width="480" and the inline width)
        // must keep a width of 480, not collapse to 0.
        var wrapperTable = FindLayoutNode(layoutRoot, n =>
            n.HtmlNode?.Type == HtmlElementType.Table &&
            n.HtmlNode!.Attributes.TryGetValue("width", out var w) && w == "480");

        Assert.NotNull(wrapperTable);
        Assert.True(wrapperTable!.WidthSet, "Wrapper table should have an explicit width");
        Assert.Equal(480, wrapperTable.Width, precision: 2);
    }

    [Fact]
    public void DisneyEmail_BodyTextWrapsAcrossMultipleWordsPerLine()
    {
        // Act
        var htmlRoot = _htmlParser.Parse(DisneyStructureHtml);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Find the layout node that actually carries the body-text LineBoxes.
        var textCell = FindLayoutNode(layoutRoot, n =>
            n.HtmlNode?.Type == HtmlElementType.TableCell &&
            n.LineBoxes.Any(l => l.Text.Length > 40));

        Assert.NotNull(textCell);

        // With a 480px wrapper and 40px padding each side the content is ~400px, so the
        // ~35 words at 20px Arial should fit several per line (2-3 lines total). One-word-
        // per-line (the original bug) would mean every line is a single short word.
        var longestLine = textCell!.LineBoxes.Max(l => l.Text.Length);
        Assert.True(longestLine > 40,
            $"Expected lines longer than 40 chars, but the longest line was {longestLine} chars across {textCell.LineBoxes.Count} lines");
    }

    [Fact]
    public void InlineStyles_ImportantSuffix_IsStrippedForEveryProperty()
    {
        // Arrange - a range of inline properties carrying !important
        var html = @"<table style=""width: 480px !important; color: #ff0000 !important; margin-left: 12px !important;"">
            <tr><td>Content</td></tr></table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var table = htmlRoot.Children.First(c => c.Type == HtmlElementType.Table);
        var style = table.Style;

        // Assert - the values are parsed as if !important were never present
        Assert.True(style.WidthSet);
        Assert.Equal(480, style.Width, precision: 2);
        Assert.Equal("#ff0000", style.Color);
        Assert.True(style.MarginLeftSet);
        Assert.Equal(12, style.MarginLeft, precision: 2);
    }

    [Fact]
    public void InlineStyles_ValueWithoutImportant_IsUnchanged()
    {
        // Arrange - plain values (no !important) must still parse identically
        var html = @"<table style=""width: 300px; color: #00ff00; margin-left: 8px;""><tr><td>Content</td></tr></table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var table = htmlRoot.Children.First(c => c.Type == HtmlElementType.Table);

        // Assert
        Assert.Equal(300, table.Style.Width, precision: 2);
        Assert.Equal("#00ff00", table.Style.Color);
        Assert.Equal(8, table.Style.MarginLeft, precision: 2);
    }
}

using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

/// <summary>
/// Tests for elements whose text is metadata rather than rendered content (style, script,
/// head metadata). Emails - especially MJML/Amazon style templates - place style blocks
/// inside table cells, and those rules must never show up as visible text.
/// </summary>
public class NonRenderedElementTests
{
    private readonly LayoutEngine _layoutEngine;
    private readonly HtmlParser _htmlParser;

    public NonRenderedElementTests()
    {
        _layoutEngine = new LayoutEngine();
        _htmlParser = new HtmlParser();
    }

    [Fact]
    public void Parse_StyleInsideTableCell_IsNotParsed()
    {
        // Arrange - the Amazon order mail pattern: a style block fostered into a table cell
        var html = "<table><tbody><tr><td>"
                   + "<style type=\"text/css\">.footer-text,.footer-card p{color:#494D4D;}@media (prefers-color-scheme:dark){.footer-card{background-color:#303333 !important;}}</style>"
                   + "<p>Order total</p>"
                   + "</td></tr></tbody></table>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);

        // Assert - the style element and its rules never reach the layout tree
        Assert.DoesNotContain(FlattenNodes(htmlRoot), n => string.Equals(n.TagName, "style", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(FlattenNodes(htmlRoot), n => (n.TextContent ?? string.Empty).Contains(".footer-text", StringComparison.Ordinal));
        Assert.Contains(FlattenNodes(htmlRoot), n => n.TextContent == "Order total");
    }

    [Fact]
    public void Layout_StyleInsideTableCell_IsNotVisibleText()
    {
        // Arrange
        var html = "<table><tbody><tr><td>"
                   + "<style>.a{color:red}b{margin:0}</style>"
                   + "<p>Delivered to Amsterdam</p>"
                   + "</td></tr></tbody></table>";

        // Act
        var layoutRoot = _layoutEngine.Layout(_htmlParser.Parse(html), 800);

        // Assert - no drawn line box contains CSS syntax, and the real content is drawn
        var lines = CollectLineTexts(layoutRoot);
        Assert.Contains("Delivered to Amsterdam", lines);
        Assert.DoesNotContain(lines, line => line.Contains('{') || line.Contains('}'));
    }

    [Fact]
    public void Parse_ScriptInBody_IsNotParsed()
    {
        // Arrange
        var html = "<div><p>Visible</p><script type=\"text/javascript\">var trackingPixel = {id: 'x'};</script></div>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);

        // Assert
        var text = Text(FlattenNodes(htmlRoot));
        Assert.Contains("Visible", text);
        Assert.DoesNotContain("trackingPixel", text);
    }

    [Fact]
    public void Parse_HeadMetadata_IsNotParsed()
    {
        // Arrange - head metadata plus a link element written in the body
        var html = "<html><head><title>Ama</title><meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">"
                   + "<link rel=\"stylesheet\" href=\"a.css\"></head>"
                   + "<body><p>Body</p><link rel=\"stylesheet\" href=\"b.css\"><base href=\"https://example.com\"></body></html>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);

        // Assert
        var text = Text(FlattenNodes(htmlRoot));
        Assert.Contains("Body", text);
        Assert.DoesNotContain("a.css", text);
        Assert.DoesNotContain("b.css", text);
    }

    [Fact]
    public void Layout_NonRenderedElements_DoNotAffectHeight()
    {
        // Arrange - the same document with and without a style block in the cell
        const string body = "<table><tbody><tr><td><p>Total</p></td></tr></tbody></table>";
        const string withStyle = "<table><tbody><tr><td>"
                                 + "<style>.x{color:red}.y{color:blue}.z{color:green}.w{color:black}</style>"
                                 + "<p>Total</p></td></tr></tbody></table>";

        // Act
        var plainHeight = _layoutEngine.Layout(_htmlParser.Parse(body), 800).Height;
        var styledHeight = _layoutEngine.Layout(_htmlParser.Parse(withStyle), 800).Height;

        // Assert - the style block adds no height
        Assert.Equal(plainHeight, styledHeight);
    }

    private static IEnumerable<HtmlNode> FlattenNodes(HtmlNode node)
    {
        yield return node;
        foreach (var child in node.Children)
        {
            foreach (var descendant in FlattenNodes(child))
                yield return descendant;
        }
    }

    private static IEnumerable<string> CollectLineTexts(LayoutNode node)
    {
        foreach (var line in node.LineBoxes)
            yield return line.Text ?? string.Empty;

        foreach (var child in node.Children)
        {
            foreach (var text in CollectLineTexts(child))
                yield return text;
        }
    }

    private static string Text(IEnumerable<HtmlNode> nodes) =>
        string.Concat(nodes.Select(n => n.TextContent ?? string.Empty));
}

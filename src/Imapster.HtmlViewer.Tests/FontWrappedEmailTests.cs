using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

/// <summary>
/// Tests for legacy &lt;font&gt;-wrapped emails (e.g. the Barnes job-ad email) that have
/// no table wrapper around the body. The &lt;font&gt; element is deprecated HTML; the parser
/// mapped it to an unknown element type, and the layout engine then skipped its content
/// entirely, so almost nothing of the email was rendered.
/// </summary>
public class FontWrappedEmailTests
{
    private readonly LayoutEngine _layoutEngine;
    private readonly HtmlParser _htmlParser;

    public FontWrappedEmailTests()
    {
        _layoutEngine = new LayoutEngine();
        _htmlParser = new HtmlParser();
    }

    private const string BarnesHtml = @"
<font face=""Geneva, Arial, Sans-serif"" size=2 color=#000000><p>Goedemiddag&nbsp;Colin,<br />
<br />
Namens een vaste klant van Barnes ben ik per direct op zoek naar een freelance Fullstack Developer. De focus ligt hierbij op de backend. Hierbij de omschrijving en de details:<br />
<br />
<strong>Freelance Fullstack Developer (Node.js &amp; Integraties)</strong><br />
Je bent in de basis een backend developer, maar een uitdaging aan de frontend ga je niet uit de weg.</p>

<p>Wat ga je doen?</p>

<ul>
	<li>Minimaal 3 tot 5 jaar ervaring als software developer, met een duidelijke focus op backendontwikkeling.</li>
	<li>Sterke kennis van Node.js en moderne JavaScript/TypeScript.</li>
</ul>

<p><strong>Details</strong></p>

<ul>
	<li>Startdatum: ZSM</li>
	<li>Werkweek: 32 tot 40 uur</li>
</ul>
Met vriendelijke groet,<br />
<em>Kind regards</em><br />
<br />
Tim Stam</font><div style='color:#999999; font-size:8pt; font-family:arial;'>This email was sent to <a style='color:#848484' href='mailto:colin@crwsolutions.nl'>colin@crwsolutions.nl</a>.</div>";

    [Fact]
    public void FontWrappedEmail_BodyIsLaidOut()
    {
        // Act
        var htmlRoot = _htmlParser.Parse(BarnesHtml);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Every <li> inside the <font> element must keep its text and get a rendered height.
        var listItems = AllLayoutNodes(layoutRoot).Where(n => n.HtmlNode?.Type == HtmlElementType.ListItem).ToList();
        Assert.NotEmpty(listItems);
        Assert.All(listItems, li =>
        {
            var hasText = AllLayoutNodes(li).Any(t => t.HtmlNode?.Type == HtmlElementType.Text && !string.IsNullOrWhiteSpace(t.HtmlNode.TextContent));
            Assert.True(hasText, "List item lost its text content");
            Assert.True(li.Height > 0, $"List item height is {li.Height} - not rendered");
        });

        // The paragraphs must be laid out too.
        var paragraphs = AllLayoutNodes(layoutRoot).Where(n => n.HtmlNode?.Type == HtmlElementType.Paragraph).ToList();
        Assert.NotEmpty(paragraphs);
        Assert.True(paragraphs.All(p => p.Height > 0), "A paragraph was not given a rendered height");

        // The root must have a sensible height for ~25+ lines of text, not just the
        // small trailing unsubscribe div.
        Assert.True(layoutRoot.Height > 300, $"Layout height too small: {layoutRoot.Height}");
    }

    [Fact]
    public void FontElement_LegacyAttributes_AreApplied()
    {
        // Arrange
        var html = @"<font face=""Geneva, Arial, Sans-serif"" size=2 color=#000000>Text</font>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var font = AllHtmlNodes(htmlRoot).FirstOrDefault(c => string.Equals(c.TagName, "font", StringComparison.OrdinalIgnoreCase));

        // Assert
        Assert.NotNull(font);
        Assert.Equal("Geneva", font!.Style.FontFamily!.Split(',')[0].Trim());
        Assert.Equal("#000000", font.Style.Color);
        // Legacy size=2 maps to the browser 'small' default (~10-12px), not 2px.
        Assert.InRange(font.Style.FontSize, 8, 14);
    }

    [Fact]
    public void FontElement_WithoutAttributes_StillLaysOut()
    {
        // Arrange
        var html = @"<font><p>Paragraph one.</p><p>Paragraph two.</p></font>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - both paragraphs are rendered
        var paragraphs = AllLayoutNodes(layoutRoot).Where(n => n.HtmlNode?.Type == HtmlElementType.Paragraph).ToList();
        Assert.Equal(2, paragraphs.Count);
        Assert.All(paragraphs, p => Assert.True(p.Height > 0));
    }

    [Fact]
    public void FontElement_InlineOnlyContent_IsLaidOut()
    {
        // Arrange - a <font> wrapping only inline content must still render its text
        var html = @"<font size=3 color=#123456>Just some plain inline text in a font tag.</font>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - the text is present in the layout tree
        var text = AllLayoutNodes(layoutRoot)
            .Where(n => n.HtmlNode?.Type == HtmlElementType.Text)
            .Select(n => n.HtmlNode!.TextContent)
            .Aggregate(string.Empty, (a, b) => a + b);
        Assert.Contains("Just some plain inline text", text);
    }

    [Fact]
    public void FontElement_TrailingInlineTextIsPlacedAfterBlocks()
    {
        // Arrange - mimics the Barnes email: paragraphs/lists inside the font, followed by
        // closing inline text ("Interesse aan meer informatie?" + signature).
        var html = @"<font size=3><p>Paragraaf één met wat tekst die een paar regels inneemt in het e-mailbericht.</p>
<ul><li>ListItem met tekst.</li></ul>
Interesse aan meer informatie?<br />
Met vriendelijke groet,</font>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Find the <font> layout node
        var fontNode = AllLayoutNodes(layoutRoot)
            .First(n => string.Equals(n.HtmlNode?.TagName, "font", StringComparison.OrdinalIgnoreCase));

        var paragraph = AllLayoutNodes(fontNode).First(n => n.HtmlNode?.Type == HtmlElementType.Paragraph);
        var list = AllLayoutNodes(fontNode).First(n => n.HtmlNode?.Type == HtmlElementType.UnorderedList);

        // The trailing inline text must have line boxes on the font node
        Assert.NotEmpty(fontNode.LineBoxes);

        // The first line of the trailing run must start BELOW the last block, not at the top
        // (where it overlaps the first paragraph).
        var lastBlockBottom = list.Y + list.Height;
        var firstLineY = fontNode.LineBoxes.Min(l => l.Y);
        Assert.True(firstLineY >= lastBlockBottom - 0.01,
            $"Trailing inline text starts at Y={firstLineY} but the last block ends at Y={lastBlockBottom}");

        // And the last line box must end at or beyond the bottom of the last block.
        var lastLineBottom = fontNode.LineBoxes.Max(l => l.Y + l.Height);
        Assert.True(lastLineBottom > paragraph.Y + paragraph.Height,
            "Trailing inline text does not extend below the first paragraph");
    }

    private static IEnumerable<LayoutNode> AllLayoutNodes(LayoutNode root)
    {
        yield return root;
        foreach (var child in root.Children)
            foreach (var descendant in AllLayoutNodes(child))
                yield return descendant;
    }

    private static IEnumerable<HtmlNode> AllHtmlNodes(HtmlNode root)
    {
        yield return root;
        foreach (var child in root.Children)
            foreach (var descendant in AllHtmlNodes(child))
                yield return descendant;
    }
}

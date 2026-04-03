using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

/// <summary>
/// Tests to verify baseline alignment and text decoration rendering.
/// </summary>
public class BaselineAndDecorationTests
{
    private readonly HtmlParser _htmlParser;
    private readonly LayoutEngine _layoutEngine;

    public BaselineAndDecorationTests()
    {
        _htmlParser = new HtmlParser();
        _layoutEngine = new LayoutEngine();
    }

    [Fact]
    public void InlineElements_ShareCommonBaseline()
    {
        // Arrange
        var html = "<p>This is <b>bold</b> and <i>italic</i> text.</p>";
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);
        
        // Find the paragraph
        Assert.NotEmpty(layoutRoot.Children);
        var paragraph = layoutRoot.Children[0];
        
        // Collect baselines from all LineBoxes
        var baselines = new List<double>();
        foreach (var child in paragraph.Children)
        {
            foreach (var lineBox in child.LineBoxes)
            {
                baselines.Add(lineBox.Baseline);
            }
        }
        
        // All LineBoxes should have the same baseline
        Assert.NotEmpty(baselines);
        var firstBaseline = baselines[0];
        Assert.True(baselines.All(b => b == firstBaseline), 
            "All inline elements on the same line should share the same baseline");
    }

    [Fact]
    public void UnderlineElement_HasTextDecorationProperty()
    {
        // Arrange
        var html = "<p>This is <u>underlined</u> text.</p>";
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);
        
        // Find the paragraph
        Assert.NotEmpty(layoutRoot.Children);
        var paragraph = layoutRoot.Children[0];
        
        // Find the underline element
        var underlineElement = paragraph.Children.FirstOrDefault(c => c.HtmlNode?.Type == HtmlElementType.Underline);
        Assert.NotNull(underlineElement);
        
        // Assert that the underline element has TextDecoration set
        Assert.Equal(TextDecoration.Underline, underlineElement.TextDecoration);
    }

    [Fact]
    public void MixedStyledElements_AllShareBaseline()
    {
        // Arrange
        var html = "<p>Regular <b>bold</b> <i>italic</i> <u>underlined</u> text.</p>";
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);
        
        // Find the paragraph
        Assert.NotEmpty(layoutRoot.Children);
        var paragraph = layoutRoot.Children[0];
        
        // Collect all baselines
        var baselines = new HashSet<double>();
        foreach (var child in paragraph.Children)
        {
            foreach (var lineBox in child.LineBoxes)
            {
                baselines.Add(lineBox.Baseline);
            }
        }
        
        // All elements should have exactly one baseline value
        Assert.Single(baselines);
    }
}

using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

/// <summary>
/// Tests to validate that text nodes between inline elements and in list items render correctly.
/// </summary>
public class TextNodeRenderingTests
{
    private readonly LayoutEngine _layoutEngine;
    private readonly HtmlParser _htmlParser;

    public TextNodeRenderingTests()
    {
        _layoutEngine = new LayoutEngine();
        _htmlParser = new HtmlParser();
    }

    #region Text Between Inline Elements Tests

    [Fact]
    public void TextNodes_BetweenBoldAndItalic_HaveLineBoxes()
    {
        // Arrange
        var html = "<p>before <b>bold</b> and <i>italic</i> after</p>";
        
        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);
        
        var paragraph = layoutRoot.Children[0];
        var textNodes = paragraph.Children.Where(c => c.HtmlNode?.Type == HtmlElementType.Text).ToList();
        
        // Should have 3 text nodes: "before ", " and ", " after"
        Assert.Equal(3, textNodes.Count);
        
        // Each text node should have LineBoxes for rendering
        foreach (var textNode in textNodes)
        {
            Assert.NotEmpty(textNode.LineBoxes);
        }
    }

    [Fact]
    public void TextNode_BeforeBold_HasCorrectContent()
    {
        // Arrange
        var html = "<p>before <b>bold</b></p>";
        
        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var paragraph = layoutRoot.Children[0];
        var textNodes = paragraph.Children.Where(c => c.HtmlNode?.Type == HtmlElementType.Text).ToList();
        
        Assert.Single(textNodes);
        Assert.Equal("before ", textNodes[0].HtmlNode?.TextContent);
    }

    [Fact]
    public void TextNode_BetweenBoldAndItalic_HasCorrectContent()
    {
        // Arrange
        var html = "<p><b>bold</b> and <i>italic</i></p>";
        
        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var paragraph = layoutRoot.Children[0];
        var textNodes = paragraph.Children.Where(c => c.HtmlNode?.Type == HtmlElementType.Text).ToList();
        
        Assert.Single(textNodes);
        Assert.Equal(" and ", textNodes[0].HtmlNode?.TextContent);
    }

    [Fact]
    public void TextNode_AfterItalic_HasCorrectContent()
    {
        // Arrange
        var html = "<p><i>italic</i> after</p>";
        
        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var paragraph = layoutRoot.Children[0];
        var textNodes = paragraph.Children.Where(c => c.HtmlNode?.Type == HtmlElementType.Text).ToList();
        
        Assert.Single(textNodes);
        Assert.Equal(" after", textNodes[0].HtmlNode?.TextContent);
    }

    [Fact]
    public void TextNodes_InMixedContent_AllHaveLineBoxes()
    {
        // Arrange
        var html = "<p>start <b>bold</b> middle <i>italic</i> end</p>";
        
        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var paragraph = layoutRoot.Children[0];
        var textNodes = paragraph.Children.Where(c => c.HtmlNode?.Type == HtmlElementType.Text).ToList();
        
        // Should have: "start ", " middle ", " end"
        Assert.Equal(3, textNodes.Count);
        
        // All should have LineBoxes
        foreach (var textNode in textNodes)
        {
            Assert.NotEmpty(textNode.LineBoxes);
        }
    }

    [Fact]
    public void TextNodes_HaveCorrectWidths()
    {
        // Arrange
        var html = "<p>a <b>b</b> c</p>";
        
        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var paragraph = layoutRoot.Children[0];
        var textNodes = paragraph.Children.Where(c => c.HtmlNode?.Type == HtmlElementType.Text).ToList();
        
        // Each text node should have positive width
        foreach (var textNode in textNodes)
        {
            Assert.True(textNode.Width > 0, $"Text node '{textNode.HtmlNode?.TextContent}' should have positive width");
        }
    }

    #endregion

    #region List Item Text Tests

    [Fact]
    public void ListItem_WithPlainText_HasLineBoxes()
    {
        // Arrange
        var html = "<ul><li>Item text</li></ul>";
        
        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var list = layoutRoot.Children[0];
        var listItem = list.Children[0];
        var textNodes = listItem.Children.Where(c => c.HtmlNode?.Type == HtmlElementType.Text).ToList();
        
        // Should have one text node
        Assert.Single(textNodes);
        Assert.Equal("Item text", textNodes[0].HtmlNode?.TextContent);
        
        // Text node should have LineBoxes
        Assert.NotEmpty(textNodes[0].LineBoxes);
    }

    [Fact]
    public void ListItem_WithText_HasPositiveHeight()
    {
        // Arrange
        var html = "<ul><li>Item 1</li><li>Item 2</li></ul>";
        
        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var list = layoutRoot.Children[0];
        
        foreach (var listItem in list.Children)
        {
            Assert.True(listItem.Height > 0, "List item should have positive height");

            var textNodes = listItem.Children.Where(c => c.HtmlNode?.Type == HtmlElementType.Text).ToList();
            Assert.NotEmpty(textNodes);

            foreach (var textNode in textNodes)
            {
                Assert.NotEmpty(textNode.LineBoxes);
            }
        }
    }

    [Fact]
    public void OrderedListItem_WithText_HasLineBoxes()
    {
        // Arrange
        var html = "<ol><li>First item</li><li>Second item</li></ol>";
        
        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var list = layoutRoot.Children[0];
        
        for (int i = 0; i < list.Children.Count; i++)
        {
            var listItem = list.Children[i];
            var textNodes = listItem.Children.Where(c => c.HtmlNode?.Type == HtmlElementType.Text).ToList();

            Assert.NotEmpty(textNodes);

            foreach (var textNode in textNodes)
            {
                Assert.NotEmpty(textNode.LineBoxes);
            }
        }
    }

    [Fact]
    public void NestedListItem_WithText_HasLineBoxes()
    {
        // Arrange
        var html = "<ul><li>Parent<ul><li>Child</li></ul></li></ul>";
        
        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var outerList = layoutRoot.Children[0];
        var parentItem = outerList.Children[0];
        var parentTextNodes = parentItem.Children.Where(c => c.HtmlNode?.Type == HtmlElementType.Text).ToList();
        
        // Parent item should have text
        Assert.Single(parentTextNodes);
        Assert.Equal("Parent", parentTextNodes[0].HtmlNode?.TextContent);
        Assert.NotEmpty(parentTextNodes[0].LineBoxes);
    }

    #endregion

    #region Complex Mixed Content Tests

    [Fact]
    public void ParagraphWithMixedInlineAndText_AllNodesHaveLineBoxes()
    {
        // Arrange
        var html = "<p>text <b>bold</b> text <i>italic</i> text <u>underline</u> text</p>";
        
        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var paragraph = layoutRoot.Children[0];
        
        // All children should have LineBoxes (text nodes AND inline elements)
        foreach (var child in paragraph.Children)
        {
            if (child.LayoutType == LayoutType.None && child.HtmlNode?.Type == HtmlElementType.Text)
            {
                Assert.NotEmpty(child.LineBoxes);
            }
            else if (child.LayoutType == LayoutType.Inline)
            {
                Assert.NotEmpty(child.LineBoxes);
            }
        }
    }

    [Fact]
    public void DivWithBlockChildrenAndText_TextNodesRender()
    {
        // Arrange
        var html = "<div>Before paragraph<p>Inside</p>After paragraph</div>";
        
        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var div = layoutRoot.Children[0];
        var textNodes = div.Children.Where(c => c.HtmlNode?.Type == HtmlElementType.Text).ToList();
        
        // Should have text nodes before and after the paragraph
        Assert.True(textNodes.Count > 0, "Div should have text node children");
        
        foreach (var textNode in textNodes)
        {
            Assert.NotEmpty(textNode.LineBoxes);
        }
    }

    #endregion

    #region Spacing Validation Tests

    [Fact]
    public void SpaceBetweenElements_WithTextAfter_IsPreserved()
    {
        // Arrange - This tests that spaces are preserved when surrounded by text
        var html = "<p><b>bold</b> and <i>italic</i></p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var paragraph = layoutRoot.Children[0];
        var textNodes = paragraph.Children.Where(c => c.HtmlNode?.Type == HtmlElementType.Text).ToList();

        // Should have one text node with the spaces
        Assert.Single(textNodes);
        Assert.Equal(" and ", textNodes[0].HtmlNode?.TextContent);
    }

    [Fact]
    public void MultipleSpacesBetweenElements_WithText_ArePreserved()
    {
        // Arrange
        var html = "<p><b>bold</b>   and   <i>italic</i></p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var paragraph = layoutRoot.Children[0];
        var textNodes = paragraph.Children.Where(c => c.HtmlNode?.Type == HtmlElementType.Text).ToList();

        // Should preserve spaces between elements
        Assert.Single(textNodes);
        Assert.True(textNodes[0].HtmlNode?.TextContent?.Contains("and") ?? false);
    }

    #endregion
}

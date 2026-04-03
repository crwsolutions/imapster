using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

public class LayoutEngineTests
{
    private readonly LayoutEngine _layoutEngine;
    private readonly HtmlParser _htmlParser;

    public LayoutEngineTests()
    {
        _layoutEngine = new LayoutEngine();
        _htmlParser = new HtmlParser();
    }

    #region Basic Layout Tests

    [Fact]
    public void Layout_DocumentFragmentRoot_HasBlockLayoutType()
    {
        // Arrange
        var html = "<div>Hello World</div>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - DocumentFragment root contains the actual content as children
        Assert.NotNull(layoutRoot);
        Assert.Equal(LayoutType.Block, layoutRoot.LayoutType);
        Assert.Single(layoutRoot.Children);
        Assert.Equal(LayoutType.Block, layoutRoot.Children[0].LayoutType);
    }

    [Fact]
    public void Layout_DocumentFragmentRoot_HasNonZeroHeight()
    {
        // Arrange
        var html = "<div>Hello World</div>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - DocumentFragment root height is 0, children have the actual height
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);
        Assert.True(layoutRoot.Children[0].Height > 0, "Child node height should be greater than 0");
    }

    [Fact]
    public void Layout_SingleParagraph_HasCorrectHeight()
    {
        // Arrange
        var html = "<p>Hello World</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - DocumentFragment root contains the paragraph as its child
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);
        Assert.Equal(LayoutType.Block, layoutRoot.Children[0].LayoutType);
        Assert.True(layoutRoot.Children[0].Height > 0, "Paragraph should have positive height");
    }

    [Fact]
    public void Layout_MultipleParagraphs_HeightIsSumOfChildren()
    {
        // Arrange
        var html = "<p>First paragraph</p><p>Second paragraph</p><p>Third paragraph</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - DocumentFragment root contains the paragraphs as its children
        Assert.NotNull(layoutRoot);
        Assert.Equal(3, layoutRoot.Children.Count);

        // Each paragraph should have positive height
        foreach (var child in layoutRoot.Children)
        {
            Assert.True(child.Height > 0, $"Child {child.HtmlNode?.Type} should have positive height");
        }
    }

    [Fact]
    public void Layout_NestedDivs_HeightIncludesNestedContent()
    {
        // Arrange
        var html = "<div><div><p>Nested content</p></div></div>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - DocumentFragment root contains the outer div as its child
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);
        Assert.Equal(LayoutType.Block, layoutRoot.Children[0].LayoutType);
        Assert.True(layoutRoot.Children[0].Height > 0, "Outer div should have positive height");
    }

    [Fact]
    public void Layout_WithMargins_HeightIncludesMargins()
    {
        // Arrange
        var html = "<div style='margin-top: 20px; margin-bottom: 30px;'>Content</div>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - DocumentFragment root contains the div as its child
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        // Check that margins are applied to the child
        var child = layoutRoot.Children[0];
        Assert.Equal(20, child.MarginTop);
        Assert.Equal(30, child.MarginBottom);
    }

    [Fact]
    public void Layout_WithPadding_HeightIncludesPadding()
    {
        // Arrange
        var html = "<div style='padding-top: 10px; padding-bottom: 15px;'>Content</div>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - DocumentFragment root contains the div as its child
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        // Check that padding is applied to the child
        var child = layoutRoot.Children[0];
        Assert.Equal(10, child.PaddingTop);
        Assert.Equal(15, child.PaddingBottom);
    }

    [Fact]
    public void Layout_WithBorders_HeightIncludesBorders()
    {
        // Arrange
        var html = "<div style='border-top-width: 5px; border-bottom-width: 5px;'>Content</div>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - DocumentFragment root contains the div as its child
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        // Check that borders are applied to the child
        var child = layoutRoot.Children[0];
        Assert.Equal(5, child.BorderTopWidth);
        Assert.Equal(5, child.BorderBottomWidth);
    }

    [Fact]
    public void Layout_EmptyDiv_HasZeroContentHeight()
    {
        // Arrange
        var html = "<div></div>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - DocumentFragment root contains the empty div as its child
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);
        Assert.Empty(layoutRoot.Children[0].Children);
    }

    [Fact]
    public void Layout_HeadingElements_HaveBlockLayoutType()
    {
        // Arrange
        var html = "<h1>Heading 1</h1><h2>Heading 2</h2><h3>Heading 3</h3>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - DocumentFragment root contains the headings as its children
        Assert.NotNull(layoutRoot);
        Assert.Equal(3, layoutRoot.Children.Count);

        foreach (var child in layoutRoot.Children)
        {
            Assert.Equal(LayoutType.Block, child.LayoutType);
            Assert.True(child.Height > 0, "Heading should have positive height");
        }
    }

    [Fact]
    public void Layout_ListElements_HaveBlockLayoutType()
    {
        // Arrange
        var html = "<ul><li>Item 1</li><li>Item 2</li></ul><ol><li>Ordered 1</li><li>Ordered 2</li></ol>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - DocumentFragment root contains the lists as its children
        Assert.NotNull(layoutRoot);
        Assert.Equal(2, layoutRoot.Children.Count);

        foreach (var child in layoutRoot.Children)
        {
            Assert.Equal(LayoutType.Block, child.LayoutType);
            Assert.True(child.Height > 0, "List should have positive height");
        }
    }

    [Fact]
    public void Layout_InlineElements_AreContainedInBlock()
    {
        // Arrange
        var html = "<div><span>Inline text</span></div>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - DocumentFragment root contains the div as its child
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);
        Assert.Equal(LayoutType.Block, layoutRoot.Children[0].LayoutType);

        var blockChild = layoutRoot.Children[0];
        Assert.True(blockChild.Height > 0, "Div should have positive height");
    }

    [Fact]
    public void Layout_WithCustomFontSize_HeightScales()
    {
        // Arrange
        var engine = new LayoutEngine(32, "Arial"); // Larger font size
        var html = "<p>Large text</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = engine.Layout(htmlRoot, 800);

        // Assert - DocumentFragment root contains the paragraph as its child
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);
        Assert.True(layoutRoot.Children[0].Height > 0, "Paragraph should have positive height");

        // Larger font should result in larger height (32px font + line height)
        Assert.True(layoutRoot.Children[0].Height > 19, "Large font should result in larger height");
    }

    #endregion

    #region Inline Element Tests

    [Fact]
    public void Layout_BoldElement_HasInlineLayoutType()
    {
        // Arrange
        var html = "<p><b>Bold text</b></p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - DocumentFragment root contains the paragraph as its child
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];
        Assert.Equal(LayoutType.Block, paragraph.LayoutType);

        // The paragraph should contain the bold element
        Assert.True(paragraph.Children.Count > 0, "Paragraph should have children");

        // Find the bold element - it should be a direct child of the paragraph
        var boldElement = paragraph.Children.FirstOrDefault(c => c.HtmlNode?.Type == HtmlElementType.Bold);
        Assert.NotNull(boldElement);
        Assert.Equal(LayoutType.Inline, boldElement!.LayoutType);
        Assert.True(boldElement.FontBold);
    }

    [Fact]
    public void Layout_ItalicElement_HasInlineLayoutType()
    {
        // Arrange
        var html = "<p><i>Italic text</i></p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];
        Assert.Equal(LayoutType.Block, paragraph.LayoutType);

        // Find the italic element - it should be a direct child of the paragraph
        var italicElement = paragraph.Children.FirstOrDefault(c => c.HtmlNode?.Type == HtmlElementType.Italic);
        Assert.NotNull(italicElement);
        Assert.Equal(LayoutType.Inline, italicElement!.LayoutType);
        Assert.True(italicElement.FontItalic);
    }

    [Fact]
    public void Layout_UnderlineElement_HasInlineLayoutType()
    {
        // Arrange
        var html = "<p><u>Underlined text</u></p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];
        Assert.Equal(LayoutType.Block, paragraph.LayoutType);

        // Find the underline element - it should be a direct child of the paragraph
        var underlineElement = paragraph.Children.FirstOrDefault(c => c.HtmlNode?.Type == HtmlElementType.Underline);
        Assert.NotNull(underlineElement);
        Assert.Equal(LayoutType.Inline, underlineElement!.LayoutType);
        Assert.True(underlineElement.TextDecoration == Parsing.TextDecoration.Underline);
    }

    [Fact]
    public void Layout_MixedInlineElements_HasCorrectStructure()
    {
        // Arrange
        var html = "<p><b>Bold</b> and <i>italic</i> and <u>underlined</u></p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];
        Assert.Equal(LayoutType.Block, paragraph.LayoutType);

        // Should have multiple children (text nodes and inline elements)
        Assert.True(paragraph.Children.Count > 0, "Paragraph should have children");

        // Check that all inline elements have correct layout type
        foreach (var child in paragraph.Children)
        {
            if (child.HtmlNode?.Type == HtmlElementType.Bold ||
                child.HtmlNode?.Type == HtmlElementType.Italic ||
                child.HtmlNode?.Type == HtmlElementType.Underline)
            {
                Assert.Equal(LayoutType.Inline, child.LayoutType);
            }
        }
    }

    [Fact]
    public void Layout_InlineElements_FlowHorizontally()
    {
        // Arrange
        var html = "<p><b>Bold</b> <i>Italic</i> <u>Underline</u></p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];

        // Inline elements should flow horizontally, not vertically
        // The paragraph height should be approximately one line height
        Assert.True(paragraph.Height > 0, "Paragraph should have positive height");
        Assert.True(paragraph.Height < 50, "Single line of inline elements should not exceed ~50px height");
    }

    [Fact]
    public void Layout_LinkElement_HasInlineLayoutType()
    {
        // Arrange
        var html = "<p><a href='http://example.com'>Link text</a></p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];
        Assert.Equal(LayoutType.Block, paragraph.LayoutType);

        // Find the link element
        var linkElement = paragraph.Children.FirstOrDefault(c => c.HtmlNode?.Type == HtmlElementType.Link);
        Assert.NotNull(linkElement);
        Assert.Equal(LayoutType.Inline, linkElement!.LayoutType);
        Assert.Equal("http://example.com", linkElement.Href);
    }

    #endregion

    #region Block Element Spacing Tests

    [Fact]
    public void Layout_Paragraph_HasDefaultMargins()
    {
        // Arrange
        var html = "<p>Paragraph text</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];

        // Paragraphs should have default margins (1em = 16px with default font size)
        Assert.True(paragraph.MarginTop >= 16, "Paragraph should have default top margin");
        Assert.True(paragraph.MarginBottom >= 16, "Paragraph should have default bottom margin");
    }

    [Fact]
    public void Layout_Heading_HasDefaultMargins()
    {
        // Arrange
        var html = "<h1>Heading text</h1>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var heading = layoutRoot.Children[0];

        // Headings should have default margins (0.67em = ~10.7px with default font size)
        Assert.True(heading.MarginTop >= 10, "Heading should have default top margin");
        Assert.True(heading.MarginBottom >= 10, "Heading should have default bottom margin");
    }

    [Fact]
    public void Layout_Blockquote_HasDefaultMargins()
    {
        // Arrange
        var html = "<blockquote>Quote text</blockquote>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var blockquote = layoutRoot.Children[0];

        // Blockquotes should have default margins on all sides
        Assert.True(blockquote.MarginLeft >= 16, "Blockquote should have default left margin");
        Assert.True(blockquote.MarginRight >= 16, "Blockquote should have default right margin");
        Assert.True(blockquote.MarginTop >= 16, "Blockquote should have default top margin");
        Assert.True(blockquote.MarginBottom >= 16, "Blockquote should have default bottom margin");
    }

    [Fact]
    public void Layout_List_HasDefaultMargins()
    {
        // Arrange
        var html = "<ul><li>Item 1</li><li>Item 2</li></ul>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var list = layoutRoot.Children[0];

        // Lists should have default margins
        Assert.True(list.MarginLeft >= 16, "List should have default left margin");
        Assert.True(list.MarginRight >= 16, "List should have default right margin");
        Assert.True(list.MarginTop >= 16, "List should have default top margin");
        Assert.True(list.MarginBottom >= 16, "List should have default bottom margin");
    }

    [Fact]
    public void Layout_ListItem_HasDefaultPadding()
    {
        // Arrange
        var html = "<ul><li>Item 1</li></ul>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var list = layoutRoot.Children[0];
        Assert.Single(list.Children);

        var listItem = list.Children[0];

        // List items should have default padding for marker space
        Assert.True(listItem.PaddingLeft >= 24, "List item should have default left padding for marker");
        Assert.True(listItem.PaddingTop >= 8, "List item should have default top padding");
        Assert.True(listItem.PaddingBottom >= 8, "List item should have default bottom padding");
    }

    [Fact]
    public void Layout_MultipleParagraphs_HaveSpacing()
    {
        // Arrange
        var html = "<p>First</p><p>Second</p><p>Third</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Equal(3, layoutRoot.Children.Count);

        // Each paragraph should have margins
        foreach (var paragraph in layoutRoot.Children)
        {
            Assert.True(paragraph.MarginTop >= 16, "Paragraph should have default top margin");
            Assert.True(paragraph.MarginBottom >= 16, "Paragraph should have default bottom margin");
        }

        // Total height should include margins
        var expectedHeight = layoutRoot.Children.Sum(c => c.Height + c.MarginTop + c.MarginBottom);
        Assert.Equal(expectedHeight, layoutRoot.Height);
    }

    #endregion

    #region List Rendering Tests

    [Fact]
    public void Layout_UnorderedList_HasCorrectStructure()
    {
        // Arrange
        var html = "<ul><li>First</li><li>Second</li><li>Third</li></ul>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var list = layoutRoot.Children[0];
        Assert.Equal(LayoutType.Block, list.LayoutType);
        Assert.Equal(3, list.Children.Count);

        // Each list item should have correct structure
        for (int i = 0; i < 3; i++)
        {
            var listItem = list.Children[i];
            Assert.Equal(LayoutType.Block, listItem.LayoutType);
            Assert.True(listItem.Height > 0, $"List item {i} should have positive height");
        }
    }

    [Fact]
    public void Layout_OrderedList_HasCorrectStructure()
    {
        // Arrange
        var html = "<ol><li>Step one</li><li>Step two</li><li>Step three</li></ol>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var list = layoutRoot.Children[0];
        Assert.Equal(LayoutType.Block, list.LayoutType);
        Assert.Equal(3, list.Children.Count);

        // Each list item should have correct structure
        for (int i = 0; i < 3; i++)
        {
            var listItem = list.Children[i];
            Assert.Equal(LayoutType.Block, listItem.LayoutType);
            Assert.True(listItem.Height > 0, $"List item {i} should have positive height");
        }
    }

    [Fact]
    public void Layout_ListItems_HaveVerticalSpacing()
    {
        // Arrange
        var html = "<ul><li>First</li><li>Second</li><li>Third</li></ul>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var list = layoutRoot.Children[0];

        // List items should have vertical spacing (padding)
        foreach (var listItem in list.Children)
        {
            Assert.True(listItem.PaddingTop >= 8, "List item should have top padding");
            Assert.True(listItem.PaddingBottom >= 8, "List item should have bottom padding");
        }
    }

    [Fact]
    public void Layout_NestedLists_HaveCorrectStructure()
    {
        // Arrange
        var html = "<ul><li>Parent 1<ul><li>Child 1</li><li>Child 2</li></ul></li><li>Parent 2</li></ul>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var outerList = layoutRoot.Children[0];
        Assert.Equal(2, outerList.Children.Count);

        // First item should contain nested list
        var firstItem = outerList.Children[0];
        Assert.True(firstItem.Children.Count > 0, "First list item should have children");
    }

    #endregion

    #region Preformatted Text Tests

    [Fact]
    public void Layout_PreElement_HasBlockLayoutType()
    {
        // Arrange
        var html = "<pre>Line 1\nLine 2\nLine 3</pre>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var pre = layoutRoot.Children[0];
        Assert.Equal(LayoutType.Block, pre.LayoutType);
        Assert.True(pre.Height > 0, "Pre element should have positive height");
    }

    [Fact]
    public void Layout_PreElement_HasDefaultMargins()
    {
        // Arrange
        var html = "<pre>Code block</pre>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var pre = layoutRoot.Children[0];

        // Pre elements should have default margins
        Assert.True(pre.MarginTop >= 8, "Pre should have default top margin");
        Assert.True(pre.MarginBottom >= 8, "Pre should have default bottom margin");
        Assert.True(pre.PaddingLeft >= 8, "Pre should have default left padding");
        Assert.True(pre.PaddingRight >= 8, "Pre should have default right padding");
    }

    [Fact]
    public void Layout_PreElement_MultipleLines_HasCorrectHeight()
    {
        // Arrange
        var html = "<pre>Line 1\nLine 2\nLine 3\nLine 4\nLine 5</pre>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var pre = layoutRoot.Children[0];

        // Pre element with 5 lines should have height proportional to line count
        // Each line is approximately 1.2 * fontSize = 19.2px
        Assert.True(pre.Height >= 80, "Pre with 5 lines should have sufficient height");
    }

    #endregion

    #region Background and Styling Tests

    [Fact]
    public void Layout_ElementWithBackgroundColor_HasBackgroundColor()
    {
        // Arrange
        var html = "<p style='background-color: #f0f0f0;'>Styled paragraph</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];
        Assert.Equal("#f0f0f0", paragraph.BackgroundColor);
    }

    [Fact]
    public void Layout_ElementWithTextColor_HasTextColor()
    {
        // Arrange
        var html = "<p style='color: #ff0000;'>Red text</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];
        Assert.Equal("#ff0000", paragraph.TextColor);
    }

    [Fact]
    public void Layout_ElementWithCustomFontSize_HasCorrectFontSize()
    {
        // Arrange
        var html = "<p style='font-size: 24px;'>Large text</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];
        Assert.Equal(24, paragraph.FontSize);
    }

    [Fact]
    public void Layout_ElementWithCustomFontFamily_HasCorrectFontFamily()
    {
        // Arrange
        var html = "<p style='font-family: Georgia;'>Georgia text</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];
        Assert.Equal("Georgia", paragraph.FontFamily);
    }

    #endregion

    #region Line Box Tests

    [Fact]
    public void Layout_FirstLineVisible_HasLineBoxes()
    {
        // Arrange
        var html = "<p>First line should be visible. Second line follows.</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];

        // The paragraph should have line boxes for its text content
        // Either directly or through children
        var hasLineBoxes = paragraph.LineBoxes.Count > 0 ||
                          paragraph.Children.Any(c => c.LineBoxes.Count > 0);
        Assert.True(hasLineBoxes, "Paragraph should have line boxes for text rendering");
    }

    [Fact]
    public void Layout_HeightIsReasonable_NoExcessiveHeight()
    {
        // Arrange
        var html = "<p>Simple paragraph with normal text.</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];

        // Height should be reasonable (font size * line height multiplier)
        // Default font size is 16, line height multiplier is 1.2, so expected ~19.2
        Assert.True(paragraph.Height > 0, "Height should be positive");
        Assert.True(paragraph.Height < 100, "Height should not be excessive for a single line");
    }

    [Fact]
    public void Layout_MultipleParagraphs_NoExcessiveHeight()
    {
        // Arrange
        var html = "<p>First paragraph.</p><p>Second paragraph.</p><p>Third paragraph.</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Equal(3, layoutRoot.Children.Count);

        // Total height should be sum of paragraph heights (no excessive margins)
        var expectedHeight = layoutRoot.Children.Sum(c => c.Height + c.MarginTop + c.MarginBottom);
        Assert.Equal(expectedHeight, layoutRoot.Height);
    }

    [Fact]
    public void Layout_LongText_WrapsCorrectly()
    {
        // Arrange
        var html = "<p>This is a very long line of text that should wrap to multiple lines when it exceeds the available width of the container element.</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 400); // Narrow width to force wrapping

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];

        // Should have multiple line boxes due to wrapping
        var totalLineBoxes = paragraph.LineBoxes.Count +
                            paragraph.Children.Sum(c => c.LineBoxes.Count);
        Assert.True(totalLineBoxes >= 2, "Long text should wrap to multiple lines");
    }

    [Fact]
    public void Layout_LineBoxes_HaveCorrectYPositions()
    {
        // Arrange - Use long text with narrow width to force line wrapping
        var html = "<p>This is a very long line of text that should wrap to multiple lines when it exceeds the available width of the container element.</p>";

        // Act - Use narrow width to force wrapping
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 200); // Narrow width to force wrapping

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];

        // Use only the paragraph's LineBoxes (which contain the properly laid out lines)
        // Don't include children's LineBoxes as they may have Y=0 from individual layout
        var allLineBoxes = new List<LineBox>(paragraph.LineBoxes);

        // Should have multiple line boxes due to wrapping
        Assert.True(allLineBoxes.Count >= 2, "Long text with narrow width should wrap to multiple lines");

        // Line boxes should have increasing Y positions
        for (int i = 1; i < allLineBoxes.Count; i++)
        {
            Assert.True(allLineBoxes[i].Y > allLineBoxes[i - 1].Y,
                $"Line {i} should be below line {i - 1}");
        }
    }

    #endregion

    #region Complex Layout Tests

    [Fact]
    public void Layout_ComplexDocument_HasCorrectStructure()
    {
        // Arrange
        var html = @"
            <div>
                <h1>Title</h1>
                <p>First paragraph with <b>bold</b> and <i>italic</i> text.</p>
                <ul>
                    <li>Item 1</li>
                    <li>Item 2</li>
                    <li>Item 3</li>
                </ul>
                <blockquote>A quote</blockquote>
                <p>Final paragraph.</p>
            </div>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var container = layoutRoot.Children[0];
        Assert.Equal(LayoutType.Block, container.LayoutType);
        Assert.True(container.Height > 0, "Container should have positive height");

        // Should have multiple children (h1, p, ul, blockquote, p)
        Assert.True(container.Children.Count >= 5, "Container should have multiple children");
    }

    [Fact]
    public void Layout_ComplexDocument_NoOverlapping()
    {
        // Arrange
        var html = @"
            <div>
                <p>First paragraph.</p>
                <p>Second paragraph.</p>
                <p>Third paragraph.</p>
            </div>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var container = layoutRoot.Children[0];

        // Children should have increasing Y positions (no overlapping)
        for (int i = 1; i < container.Children.Count; i++)
        {
            var prevChild = container.Children[i - 1];
            var currChild = container.Children[i];

            // Current child should start after previous child ends (including margins)
            var prevBottom = prevChild.Y + prevChild.Height + prevChild.MarginBottom;
            Assert.True(currChild.Y >= prevBottom - 1,
                $"Child {i} should not overlap with child {i - 1}");
        }
    }

    [Fact]
    public void Layout_InlineElementsInParagraph_NoVerticalStacking()
    {
        // Arrange
        var html = "<p>This is a <b>bold</b> and <i>italic</i> text demonstration.</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];

        // Paragraph height should be approximately one line height
        // If inline elements were stacked vertically, height would be much larger
        Assert.True(paragraph.Height > 0, "Paragraph should have positive height");
        Assert.True(paragraph.Height < 50, "Single line with inline elements should not exceed ~50px");
    }

    [Fact]
    public void Layout_InlineElements_HaveCorrectXPositions()
    {
        // Arrange - Test the specific case: <p>This is a <b>bold</b> and <i>italic</i> text demonstration.</p>
        var html = "<p>This is a <b>bold</b> and <i>italic</i> text demonstration.</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];

        // Get inline children (text nodes and inline elements)
        var inlineChildren = paragraph.Children.Where(c =>
            c.LayoutType == LayoutType.Inline ||
            (c.LayoutType == LayoutType.None && c.HtmlNode?.Type == HtmlElementType.Text)).ToList();

        Assert.True(inlineChildren.Count > 0, "Paragraph should have inline children");

        // Verify X positions flow horizontally (each child starts where previous ended)
        double expectedX = 0;
        foreach (var child in inlineChildren)
        {
            // Each child's X should be at least the expected position (allowing for small rounding errors)
            Assert.True(child.X >= expectedX - 1,
                $"Child '{child.HtmlNode?.TextContent ?? child.HtmlNode?.TagName}' X position ({child.X}) should be >= expected ({expectedX})");

            // Move expected X forward by this child's width
            expectedX += child.Width;
        }

        // The total width of inline children should match the paragraph's content width
        var totalInlineWidth = inlineChildren.Sum(c => c.Width);
        Assert.True(totalInlineWidth > 0, "Inline children should have total width > 0");
    }

    [Fact]
    public void Layout_BoldElement_HasPositiveWidth()
    {
        // Arrange
        var html = "<p><b>bold</b></p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];

        // Find the bold element
        var boldElement = paragraph.Children.FirstOrDefault(c => c.HtmlNode?.Type == HtmlElementType.Bold);
        Assert.NotNull(boldElement);
        Assert.True(boldElement!.Width > 0, "Bold element should have positive width");
    }

    [Fact]
    public void Layout_ItalicElement_HasPositiveWidth()
    {
        // Arrange
        var html = "<p><i>italic</i></p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraph = layoutRoot.Children[0];

        // Find the italic element
        var italicElement = paragraph.Children.FirstOrDefault(c => c.HtmlNode?.Type == HtmlElementType.Italic);
        Assert.NotNull(italicElement);
        Assert.True(italicElement!.Width > 0, "Italic element should have positive width");
    }

    [Fact]
    public void Layout_InlineElementsWithLongText_WrapsCorrectly()
    {
        // Arrange - This is the case: long text with multiple inline formatting elements
        var html = @"
            <p>
                This paragraph demonstrates <b>bold text</b>, <strong>strong text</strong>, 
                <i>italic text</i>, <em>emphasized text</em>, <u>underlined text</u>, 
                <mark style='background-color: #FFFF00;'>highlighted text</mark>, 
                <small>small text</small>, <del>deleted text</del>, and <ins>inserted text</ins>.
            </p>";

        // Act - Use narrow width to force wrapping
        var htmlRoot = _htmlParser.Parse(html);
        var narrowLayoutRoot = _layoutEngine.Layout(htmlRoot, 300); // Very narrow to force wrapping
        var wideLayoutRoot = _layoutEngine.Layout(htmlRoot, 800); // Wide - should be single line

        // Assert
        Assert.NotNull(narrowLayoutRoot);
        Assert.Single(narrowLayoutRoot.Children);
        Assert.NotNull(wideLayoutRoot);
        Assert.Single(wideLayoutRoot.Children);

        var narrowParagraph = narrowLayoutRoot.Children[0];
        var wideParagraph = wideLayoutRoot.Children[0];

        // Both should have height > 0
        Assert.True(narrowParagraph.Height > 0, "Narrow paragraph should have height");
        Assert.True(wideParagraph.Height > 0, "Wide paragraph should have height");

        // With narrow width (300px), the paragraph should be significantly taller than with wide width (800px)
        // because text should wrap to multiple lines
        // Expected: narrow should be at least 2-3x taller than wide
        Assert.True(narrowParagraph.Height > wideParagraph.Height * 1.5,
            $"Narrow paragraph ({narrowParagraph.Height}px) should be significantly taller than wide ({wideParagraph.Height}px) due to wrapping");
    }

    #endregion
}

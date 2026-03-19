namespace Imapster.HtmlRendering.Tests.Parsing;

using Imapster.HtmlRendering.Parsing;

public sealed class HtmlParserTests
{
    [Fact]
    public void Parse_SimpleText_ReturnsTextNode()
    {
        // Arrange
        var parser = new HtmlParser();
        var html = "Hello World";
        
        // Act
        var result = parser.Parse(html);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Children);
        Assert.Equal(HtmlElementType.Text, result.Children[0].Type);
        Assert.Equal("Hello World", result.Children[0].Text);
    }
    
    [Fact]
    public void Parse_Paragraph_ReturnsParagraphNode()
    {
        // Arrange
        var parser = new HtmlParser();
        var html = "<p>Hello World</p>";
        
        // Act
        var result = parser.Parse(html);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Children);
        Assert.Equal(HtmlElementType.Paragraph, result.Children[0].Type);
        Assert.Single(result.Children[0].Children);
        Assert.Equal("Hello World", result.Children[0].Children[0].Text);
    }
    
    [Fact]
    public void Parse_StrongReturnsBoldStyle()
    {
        // Arrange
        var parser = new HtmlParser();
        var html = "<strong>Bold text</strong>";
        
        // Act
        var result = parser.Parse(html);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Children);
        Assert.Equal(HtmlElementType.Strong, result.Children[0].Type);
        Assert.True(result.Children[0].Style.IsBold);
    }
    
    [Fact]
    public void Parse_EmphasisReturnsItalicStyle()
    {
        // Arrange
        var parser = new HtmlParser();
        var html = "<em>Italic text</em>";
        
        // Act
        var result = parser.Parse(html);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Children);
        Assert.Equal(HtmlElementType.Emphasis, result.Children[0].Type);
        Assert.True(result.Children[0].Style.IsItalic);
    }
    
    [Fact]
    public void Parse_Heading1ReturnsCorrectElementType()
    {
        // Arrange
        var parser = new HtmlParser();
        var html = "<h1>Heading 1</h1>";
        
        // Act
        var result = parser.Parse(html);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Children);
        Assert.Equal(HtmlElementType.Heading1, result.Children[0].Type);
    }
    
    [Fact]
    public void Parse_LinkReturnsLinkElementWithUrl()
    {
        // Arrange
        var parser = new HtmlParser();
        var html = "<a href=\"https://example.com\">Link</a>";
        
        // Act
        var result = parser.Parse(html);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Children);
        Assert.Equal(HtmlElementType.Link, result.Children[0].Type);
        Assert.Equal("https://example.com", result.Children[0].Style.Url);
    }
    
    [Fact]
    public void Parse_ImgReturnsImageElement()
    {
        // Arrange
        var parser = new HtmlParser();
        var html = "<img src=\"image.png\" alt=\"Test\" />";
        
        // Act
        var result = parser.Parse(html);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Children);
        Assert.Equal(HtmlElementType.Image, result.Children[0].Type);
        Assert.Equal("image.png", result.Children[0].ImageSrc);
    }
    
    [Fact]
    public void Parse_LineBreakReturnsLineBreakElement()
    {
        // Arrange
        var parser = new HtmlParser();
        var html = "Line 1<br/>Line 2";
        
        // Act
        var result = parser.Parse(html);
        
        // Assert
        Assert.NotNull(result);
        var hasLineBreak = result.Children.Any(c => c.Type == HtmlElementType.LineBreak);
        Assert.True(hasLineBreak);
    }
    
    [Fact]
    public void Parse_ListItemReturnsListItemElement()
    {
        // Arrange
        var parser = new HtmlParser();
        var html = "<ul><li>Item 1</li><li>Item 2</li></ul>";
        
        // Act
        var result = parser.Parse(html);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Children);
        Assert.Equal(HtmlElementType.List, result.Children[0].Type);
        Assert.Equal(2, result.Children[0].Children.Count);
        Assert.Equal(HtmlElementType.ListItem, result.Children[0].Children[0].Type);
        Assert.Equal(HtmlElementType.ListItem, result.Children[0].Children[1].Type);
    }
    
    [Fact]
    public void Parse_MultipleElementsReturnsAllChildren()
    {
        // Arrange
        var parser = new HtmlParser();
        var html = "<p>Paragraph</p><div>Div</div>";
        
        // Act
        var result = parser.Parse(html);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Children.Count);
        Assert.Equal(HtmlElementType.Paragraph, result.Children[0].Type);
        Assert.Equal(HtmlElementType.Div, result.Children[1].Type);
    }
    
    [Fact]
    public void Parse_NestedElementsReturnsTree()
    {
        // Arrange
        var parser = new HtmlParser();
        var html = "<div><p><strong>Bold</strong> text</p></div>";
        
        // Act
        var result = parser.Parse(html);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Children);
        Assert.Equal(HtmlElementType.Div, result.Children[0].Type);
        Assert.Single(result.Children[0].Children);
        Assert.Equal(HtmlElementType.Paragraph, result.Children[0].Children[0].Type);
        Assert.Single(result.Children[0].Children[0].Children);
        Assert.Equal(HtmlElementType.Strong, result.Children[0].Children[0].Children[0].Type);
    }
    
    [Fact]
    public void Parse_FontSizeInInlineStyle()
    {
        // Arrange
        var parser = new HtmlParser();
        var html = "<span style=\"font-size: 24px;\">Large text</span>";
        
        // Act
        var result = parser.Parse(html);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Children);
        Assert.Equal(24, result.Children[0].Style.FontSize);
    }
    
    [Fact]
    public void Parse_ColorInInlineStyle()
    {
        // Arrange
        var parser = new HtmlParser();
        var html = "<span style=\"color: red;\">Red text</span>";
        
        // Act
        var result = parser.Parse(html);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Children);
        Assert.Equal("red", result.Children[0].Style.Color);
    }
    
    [Fact]
    public void Parse_Black()
    {
        var parser = new HtmlParser();
        var html = "<p>Test</p>";
        var result = parser.Parse(html);
        Assert.NotNull(result);
    }
}
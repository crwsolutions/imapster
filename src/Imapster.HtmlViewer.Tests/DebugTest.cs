using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

public class DebugTest
{
    [Fact]
    public void Debug_ParserOutput()
    {
        // Arrange
        var html = "<p>Hello World</p>";
        var parser = new HtmlParser();

        // Act
        var htmlRoot = parser.Parse(html);

        // Debug: Print the tree structure
        PrintTree(htmlRoot, 0);

        // Assert - DocumentFragment should have the paragraph as its direct child
        Assert.NotNull(htmlRoot);
        Assert.Equal(HtmlElementType.DocumentFragment, htmlRoot.Type);
        Assert.Single(htmlRoot.Children);

        var paragraph = htmlRoot.Children[0];
        Assert.Equal(HtmlElementType.Paragraph, paragraph.Type);
        Assert.Equal("P", paragraph.TagName);
    }

    [Fact]
    public void Debug_LayoutOutput()
    {
        // Arrange
        var html = "<p>Hello World</p>";
        var parser = new HtmlParser();
        var layoutEngine = new LayoutEngine();

        // Act
        var htmlRoot = parser.Parse(html);
        var layoutRoot = layoutEngine.Layout(htmlRoot, 800);

        // Debug: Print the layout tree
        PrintLayoutTree(layoutRoot, 0);

        // Assert
        Assert.NotNull(layoutRoot);
        Assert.Single(layoutRoot.Children);

        var paragraphLayout = layoutRoot.Children[0];
        System.Console.WriteLine($"Paragraph height: {paragraphLayout.Height}");
        Assert.True(paragraphLayout.Height > 0, "Paragraph should have positive height");
    }

    private void PrintTree(HtmlNode node, int indent)
    {
        var indentStr = new string(' ', indent);
        System.Console.WriteLine($"{indentStr}Node: Type={node.Type}, TagName={node.TagName}, TextContent='{node.TextContent}'");

        foreach (var child in node.Children)
        {
            PrintTree(child, indent + 2);
        }
    }

    private void PrintLayoutTree(LayoutNode node, int indent)
    {
        var indentStr = new string(' ', indent);
        System.Console.WriteLine($"{indentStr}LayoutNode: Type={node.HtmlNode?.Type}, LayoutType={node.LayoutType}, Height={node.Height}, Width={node.Width}");

        foreach (var child in node.Children)
        {
            PrintLayoutTree(child, indent + 2);
        }
    }
}
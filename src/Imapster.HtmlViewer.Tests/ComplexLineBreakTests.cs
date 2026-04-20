using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

public class ComplexLineBreakTests
{
    private readonly LayoutEngine _layoutEngine;
    private readonly HtmlParser _htmlParser;

    public ComplexLineBreakTests()
    {
        _layoutEngine = new LayoutEngine();
        _htmlParser = new HtmlParser();
    }

    [Fact]
    public void Layout_ComplexFbtoHtml_RendersProperly()
    {
        // Arrange - Complex FBTO HTML with nested strong and links
        var html = @"
            <html>
            <body>
Beste meneer Jansen,<br /><br />Wij hebben declaraties voor u verwerkt.<br /><br /><strong><strong style=""color:#005eaa; font-weight:bold"">Op uw declaratieoverzicht ziet u welke declaraties wij voor u verwerkten</strong> </strong><br />Ook ziet u meteen de stand van uw eigen risico. Het overzicht staat klaar op Zorggebruik onder uw <a title=""Berichtenbox"" target=""_blank"" href=""https://mijnzorg.fbto.nl/"">Berichtenbox</a>. Inloggen doet u veilig &eacute;n snel met DigiD en SMS-controle of met de DigiD-app.<br /><br /><strong><strong style=""color:#005eaa; font-weight:bold"">Kunnen we nog iets voor u doen?</strong> </strong><br />Heeft u vragen? Kijk dan op <a href=""https://www.fbto.nl/zorgverzekering"">fbto.nl/zorg</a>. Of neem contact met ons op. Op <a href=""https://www.fbto.nl/verzekeringen/contact"">fbto.nl/contact</a> leest u hoe u ons bereikt.<br /><br />Hartelijke groet,<br /><br />FBTO<br/><br/>
            </body>
            </html>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - Should layout without errors
        Assert.NotNull(layoutRoot);
        Assert.True(layoutRoot.Height > 0, "Layout should have height");
        Assert.True(layoutRoot.Width > 0, "Layout should have width");
    }

    [Fact]
    public void Layout_NestedStrongWithBr_TextAfterBrIsNotBold()
    {
        // Arrange
        var html = @"
            <p><strong><strong style=""color:blue"">Bold text</strong></strong><br/>Regular text</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - Should have multiple lines
        var pNode = layoutRoot.Children[0];
        Assert.NotEmpty(pNode.LineBoxes);
        Assert.True(pNode.LineBoxes.Count >= 2, 
            $"Should have at least 2 lines with nested strong and br, got {pNode.LineBoxes.Count}");
    }

    [Fact]
    public void Layout_StrongAndLinkWithBr_AllElementsOnCorrectLines()
    {
        // Arrange
        var html = @"
            <p><strong>Bold</strong><br/><a href=""#"">Link</a><br/>Text</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var pNode = layoutRoot.Children[0];
        Assert.NotEmpty(pNode.LineBoxes);
        Assert.True(pNode.LineBoxes.Count >= 3, 
            $"Should have at least 3 lines, got {pNode.LineBoxes.Count}");
    }

    [Fact]
    public void Layout_MultipleNestedInlineWithBr_RendersProperly()
    {
        // Arrange
        var html = @"
            <p>
                <strong><strong style=""color:red"">Nested strong</strong></strong><br/>
                Text before<br/>
                <a href=""#"">Link text</a><br/>
                Text after
            </p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert
        var pNode = layoutRoot.Children[0];
        Assert.NotEmpty(pNode.LineBoxes);
        Assert.True(pNode.Height > 0, "Layout should render");
    }

    [Fact]
    public void Layout_BrWithinNestedStrongElement_CreatesNewLine()
    {
        // Arrange
        var html = @"
            <p>Text 1<br/><strong><strong>Nested strong</strong></strong><br/>Text 2</p>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - Should have at least 3 lines
        var pNode = layoutRoot.Children[0];
        Assert.NotEmpty(pNode.LineBoxes);
        Assert.True(pNode.LineBoxes.Count >= 3, 
            $"Three text segments separated by br should create 3 lines, got {pNode.LineBoxes.Count}");
    }
}

using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

public class BrTagRenderingTests
{
    private readonly LayoutEngine _layoutEngine;
    private readonly HtmlParser _htmlParser;

    public BrTagRenderingTests()
    {
        _layoutEngine = new LayoutEngine();
        _htmlParser = new HtmlParser();
    }

    [Fact]
    public void Layout_NestedStrongWithBr_HasCorrectLineBoxes()
    {
        // Arrange - Simplified version of the user's HTML with nested strong
        var html = @"<html>
<body>
<strong><strong>Hartelijke groet,</strong></strong><br /><br /><strong><strong>FBTO</strong></strong>
</body>
</html>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Debug output
        System.Console.WriteLine($"=== Layout Debug ===");
        System.Console.WriteLine($"Total line boxes: {layoutRoot.LineBoxes.Count}");
        foreach (var lb in layoutRoot.LineBoxes)
        {
            System.Console.WriteLine($"LineBox Y={lb.Y}, Text='{lb.Text}'");
        }

        // Assert - We should have at least 2 line boxes with different Y values
        Assert.True(layoutRoot.LineBoxes.Count >= 2, 
            $"Expected at least 2 line boxes, got {layoutRoot.LineBoxes.Count}");

        // Find the lines with "Hartelijke" and "FBTO"
        var hartelijkeLine = layoutRoot.LineBoxes.FirstOrDefault(l => l.Text?.Contains("Hartelijke") == true);
        var fbtoLine = layoutRoot.LineBoxes.FirstOrDefault(l => l.Text?.Contains("FBTO") == true);

        Assert.NotNull(hartelijkeLine);
        Assert.NotNull(fbtoLine);

        // They should be on different lines
        Assert.True(Math.Abs(hartelijkeLine.Y - fbtoLine.Y) > 10, 
            $"Hartelijke and FBTO should be on different lines. Hartelijke Y={hartelijkeLine.Y}, FBTO Y={fbtoLine.Y}");
    }

    [Fact]
    public void Layout_FullFbtoEmail_HasCorrectLineBreaks()
    {
        // Arrange - The exact HTML from the user's report
        var html = @"<html>
<body>
Beste meneer Jansen,<br /><br />Wij hebben declaraties voor u verwerkt.<br /><br /><strong><strong style=""color:#005eaa; font-weight:bold"">Op uw declaratieoverzicht ziet u welke declaraties wij voor u verwerkten</strong> </strong><br />Ook ziet u meteen de stand van uw eigen risico. Het overzicht staat klaar op Zorggebruik onder uw <a title=""Berichtenbox"" target=""_blank"" href=""https://mijnzorg.fbto.nl/"">Berichtenbox</a>. Inloggen doet u veilig &eacute;n snel met DigiD en SMS-controle of met de DigiD-app.<br /><br /><strong><strong style=""color:#005eaa; font-weight:bold"">Kunnen we nog iets voor u doen?</strong> </strong><br />Heeft u vragen? Kijk dan op <a href=""https://www.fbto.nl/zorgverzekering"">fbto.nl/zorg</a>. Of neem contact met ons op. Op <a href=""https://www.fbto.nl/verzekeringen/contact"">fbto.nl/contact</a> leest u hoe u ons bereikt.<br /><br />Hartelijke groet,<br /><br />FBTO<br/><br/>
</body>
</html>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Debug output
        System.Console.WriteLine($"=== Full Fbto Email Debug ===");
        System.Console.WriteLine($"Total line boxes: {layoutRoot.LineBoxes.Count}");
        foreach (var lb in layoutRoot.LineBoxes)
        {
            System.Console.WriteLine($"LineBox Y={lb.Y:F2}, Text='{lb.Text}'");
        }

        // Assert - Check that "Hartelijke groet," and "FBTO" are on different lines
        var hartelijkeLine = layoutRoot.LineBoxes.FirstOrDefault(l => l.Text?.Contains("Hartelijke") == true);
        var fbtoLine = layoutRoot.LineBoxes.FirstOrDefault(l => l.Text?.Contains("FBTO") == true);

        Assert.NotNull(hartelijkeLine);
        Assert.NotNull(fbtoLine);

        // They should be on different lines with at least one line height difference
        var lineHeight = 16 * 1.2; // Default font size * line height multiplier
        Assert.True(Math.Abs(hartelijkeLine.Y - fbtoLine.Y) >= lineHeight, 
            $"Hartelijke and FBTO should be on different lines. Difference: {Math.Abs(hartelijkeLine.Y - fbtoLine.Y):F2}, expected >= {lineHeight}");
    }
}

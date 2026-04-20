using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

public class FbtoEmailBugTest
{
    private readonly LayoutEngine _layoutEngine;
    private readonly HtmlParser _htmlParser;

    public FbtoEmailBugTest()
    {
        _layoutEngine = new LayoutEngine();
        _htmlParser = new HtmlParser();
    }

    [Fact]
    public void Layout_FbtoEmail_MultipleLinesRenderedCorrectly()
    {
        // Arrange - The exact HTML from the bug report
        var html = @"<body>
Beste meneer Jansen,<br /><br />Wij hebben declaraties voor u verwerkt.<br /><br /><strong><strong style=""color:#005eaa; font-weight:bold"">Op uw declaratieoverzicht ziet u welke declaraties wij voor u verwerkten</strong> </strong><br />Ook ziet u meteen de stand van uw eigen risico. Het overzicht staat klaar op Zorggebruik onder uw <a title=""Berichtenbox"" target=""_blank"" href=""https://mijnzorg.fbto.nl/"">Berichtenbox</a>. Inloggen doet u veilig &eacute;n snel met DigiD en SMS-controle of met de DigiD-app.<br /><br /><strong><strong style=""color:#005eaa; font-weight:bold"">Kunnen we nog iets voor u doen?</strong> </strong><br />Heeft u vragen? Kijk dan op <a href=""https://www.fbto.nl/zorgverzekering"">fbto.nl/zorg</a>. Of neem contact met ons op. Op <a href=""https://www.fbto.nl/verzekeringen/contact"">fbto.nl/contact</a> leest u hoe u ons bereikt.<br /><br />Hartelijke groet,<br /><br />FBTO<br/><br/>
</body>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // The layoutRoot is a DocumentFragment that contains the body content directly
        // Use the DocumentFragment as the root node for line boxes
        var rootNode = layoutRoot;

        var lineBoxes = rootNode.LineBoxes;
        
        // Debug: Print line information
        var debugInfo = new System.Text.StringBuilder();
        debugInfo.AppendLine($"Total line boxes: {lineBoxes.Count}");
        for (int i = 0; i < lineBoxes.Count; i++)
        {
            var line = lineBoxes[i];
            debugInfo.AppendLine($"Line {i}: Y={line.Y:F2}, Text='{line.Text?.Trim()}'");
        }

        // The bug: "Hartelijke groet," and "FBTO" should be on different lines
        // Find the lines containing "Hartelijke groet" and "FBTO"
        var hartelijkeGroetLine = lineBoxes.FirstOrDefault(l => l.Text?.Contains("Hartelijke groet") == true);
        var fbtoLine = lineBoxes.FirstOrDefault(l => l.Text?.Contains("FBTO") == true);

        Assert.NotNull(hartelijkeGroetLine);
        Assert.NotNull(fbtoLine);

        // They should be on different lines (different Y positions)
        Assert.True(Math.Abs(hartelijkeGroetLine!.Y - fbtoLine!.Y) > 0.001, 
            $"Hartelijke groet and FBTO should be on different lines. Y positions: {hartelijkeGroetLine.Y:F2} vs {fbtoLine.Y:F2}");
    }

    [Fact]
    public void Layout_FbtoEmail_Simplified_MultipleLinesRenderedCorrectly()
    {
        // Arrange - Simplified version focusing on the end
        var html = @"<body>
Test1<br /><br />Test2<br /><br />Test3<br /><br />Hartelijke groet,<br /><br />FBTO<br/><br/>
</body>";

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // The layoutRoot is a DocumentFragment that contains the body content directly
        var rootNode = layoutRoot;
        var lineBoxes = rootNode.LineBoxes;
        
        // Find the lines containing "Hartelijke groet" and "FBTO"
        var hartelijkeGroetLine = lineBoxes.FirstOrDefault(l => l.Text?.Contains("Hartelijke groet") == true);
        var fbtoLine = lineBoxes.FirstOrDefault(l => l.Text?.Contains("FBTO") == true);

        Assert.NotNull(hartelijkeGroetLine);
        Assert.NotNull(fbtoLine);

        // They should be on different lines (different Y positions)
        Assert.True(Math.Abs(hartelijkeGroetLine!.Y - fbtoLine!.Y) > 0.001, 
            $"Hartelijke groet and FBTO should be on different lines. Y positions: {hartelijkeGroetLine.Y:F2} vs {fbtoLine.Y:F2}");
    }
}

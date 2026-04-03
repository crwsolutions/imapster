using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

public class DebugTableTest
{
    [Fact]
    public void Debug_TableParsing()
    {
        var htmlParser = new HtmlParser();
        var html = @"
            <table>
                <tr>
                    <td>Cell 1</td>
                    <td>Cell 2</td>
                </tr>
                <tr>
                    <td>Row 2 Cell 1</td>
                </tr>
            </table>";

        var htmlRoot = htmlParser.Parse(html);
        
        // Debug output
        System.Console.WriteLine($"Root Type: {htmlRoot.Type}");
        System.Console.WriteLine($"Root Children Count: {htmlRoot.Children.Count}");
        
        for (int i = 0; i < htmlRoot.Children.Count; i++)
        {
            var child = htmlRoot.Children[i];
            System.Console.WriteLine($"  Child[{i}] Type: {child.Type}, TagName: {child.TagName}");
            System.Console.WriteLine($"    Child[{i}] Children Count: {child.Children.Count}");
            
            for (int j = 0; j < child.Children.Count; j++)
            {
                var grandchild = child.Children[j];
                System.Console.WriteLine($"      Grandchild[{j}] Type: {grandchild.Type}, TagName: {grandchild.TagName}");
                System.Console.WriteLine($"        Grandchild[{j}] Children Count: {grandchild.Children.Count}");
                
                for (int k = 0; k < grandchild.Children.Count; k++)
                {
                    var greatGrandchild = grandchild.Children[k];
                    System.Console.WriteLine($"          GreatGrandchild[{k}] Type: {greatGrandchild.Type}, TagName: {greatGrandchild.TagName}");
                }
            }
        }
        
        Assert.True(true); // Just to make the test pass
    }
}
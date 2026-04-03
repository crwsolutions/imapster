using CommunityToolkit.Mvvm.ComponentModel;

namespace Imapster.HtmlViewer.Demo.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _html = @"
            <h1 style='color: #CC0000; background-color: #FFFFCC; padding: 10px;'>Complete HTML Viewer Demo</h1>
            
            <h2 style='color: #0066CC; margin-top: 15px;'>Text Formatting</h2>
            <p>
                This paragraph demonstrates <b>bold text</b>, <strong>strong text</strong>, 
                <i>italic text</i>, <em>emphasized text</em>, <u>underlined text</u>, 
                <mark style='background-color: #FFFF00;'>highlighted text</mark>, 
                <small>small text</small>, <del>deleted text</del>, and <ins>inserted text</ins>.
            </p>
            <p style='color: #FF6600;'>
                Also featuring: <code>inline code</code>, <kbd>keyboard</kbd>, <samp>sample output</samp>
            </p>

            <h2 style='color: #0066CC;'>Subscript and Superscript</h2>
            <p>Chemical formula: H<sub>2</sub>O and E=mc<sup>2</sup></p>

            <h2 style='color: #0066CC;'>Links and Abbreviations</h2>
            <p>
                Visit <a href='https://example.com'>Example Website</a> or 
                <a href='https://github.com'>GitHub</a>. 
                <abbr title='HyperText Markup Language'>HTML</abbr> is awesome!
            </p>

            <h2 style='color: #0066CC;'>Unordered List (Bullets)</h2>
            <ul>
                <li>First item with some text</li>
                <li>Second item with <b>bold content</b></li>
                <li>Third item with <i>italic content</i></li>
                <li>Fourth item with <a href='#'>a link</a></li>
            </ul>

            <h2 style='color: #0066CC;'>Ordered List (Numbers)</h2>
            <ol>
                <li>Step one: Setup</li>
                <li>Step two: Configure</li>
                <li>Step three: Deploy</li>
                <li>Step four: Monitor</li>
            </ol>

            <h2 style='color: #0066CC;'>Blockquote</h2>
            <blockquote style='border-left-width: 4px; border-left-color: #0066CC; padding-left: 15px; color: #555555;'>
                <p>The only way to do great work is to love what you do.</p>
                <p>— Steve Jobs</p>
            </blockquote>

            <h2 style='color: #0066CC;'>Code Block (Preformatted)</h2>
            <pre style='background-color: #f4f4f4; padding: 10px; color: #333333; border-left-width: 3px; border-left-color: #0066CC;'>
public class HelloWorld {
    public static void Main() {
        Console.WriteLine(""Hello, World!"");
    }
}
            </pre>

            <h2 style='color: #0066CC;'>Highlighted Boxes</h2>
            <p style='background-color: #E8F4F8; padding: 10px; color: #006699;'>
                ℹ️ This is an informational box with light blue background
            </p>
            <p style='background-color: #FFF3CD; padding: 10px; color: #856404;'>
                ⚠️ This is a warning box with light yellow background
            </p>
            <p style='background-color: #F8D7DA; padding: 10px; color: #721C24;'>
                ❌ This is an error box with light red background
            </p>
            <p style='background-color: #D4EDDA; padding: 10px; color: #155724;'>
                ✓ This is a success box with light green background
            </p>

            <h2 style='color: #0066CC;'>Inline Styling Examples</h2>
            <p>
                <span style='color: #FF0000;'>Red text</span>,
                <span style='color: #00AA00;'>Green text</span>,
                <span style='color: #0000FF;'>Blue text</span>
            </p>
            <p>
                <span style='background-color: #FFFF00; color: #000000;'>Yellow highlight</span>
                <span style='background-color: #00CCFF; color: #000000;'>Cyan highlight</span>
            </p>

            <h2 style='color: #0066CC;'>Headings</h2>
            <h3 style='color: #009900;'>Heading 3</h3>
            <h4 style='color: #009900;'>Heading 4</h4>
            <h5 style='color: #009900;'>Heading 5</h5>
            <h6 style='color: #009900;'>Heading 6</h6>

            <h2 style='color: #0066CC;'>Horizontal Rule</h2>
            <hr style='border-top-width: 2px; border-top-color: #CCCCCC;' />

            <h2 style='color: #0066CC;'>Address Block</h2>
            <address style='color: #555555; font-style: italic;'>
                CRW Solutions<br />
                123 Technology Street<br />
                San Francisco, CA 94105
            </address>

            <h2 style='color: #0066CC;'>Table Example</h2>
            <table style='width: 100%; border-top-width: 1px; border-top-color: #CCCCCC; border-bottom-width: 1px; border-bottom-color: #CCCCCC;'>
                <tr style='background-color: #F0F0F0;'>
                    <th style='padding: 8px; text-align: left; border-bottom-width: 1px; border-bottom-color: #CCCCCC; color: #0066CC;'>Product</th>
                    <th style='padding: 8px; text-align: left; border-bottom-width: 1px; border-bottom-color: #CCCCCC; color: #0066CC;'>Price</th>
                    <th style='padding: 8px; text-align: left; border-bottom-width: 1px; border-bottom-color: #CCCCCC; color: #0066CC;'>Status</th>
                </tr>
                <tr>
                    <td style='padding: 8px; border-bottom-width: 1px; border-bottom-color: #E0E0E0;'>Premium Plan</td>
                    <td style='padding: 8px; border-bottom-width: 1px; border-bottom-color: #E0E0E0;'>$99/month</td>
                    <td style='padding: 8px; border-bottom-width: 1px; border-bottom-color: #E0E0E0; color: #155724;'>✓ Active</td>
                </tr>
                <tr style='background-color: #F9F9F9;'>
                    <td style='padding: 8px; border-bottom-width: 1px; border-bottom-color: #E0E0E0;'>Professional Plan</td>
                    <td style='padding: 8px; border-bottom-width: 1px; border-bottom-color: #E0E0E0;'>$49/month</td>
                    <td style='padding: 8px; border-bottom-width: 1px; border-bottom-color: #E0E0E0; color: #155724;'>✓ Active</td>
                </tr>
                <tr>
                    <td style='padding: 8px;'>Basic Plan</td>
                    <td style='padding: 8px;'>$19/month</td>
                    <td style='padding: 8px; color: #721C24;'>❌ Deprecated</td>
                </tr>
            </table>

            <h2 style='color: #0066CC;'>Text Alignment</h2>
            <p style='text-align: center; color: #0066CC;'>This text is center-aligned</p>
            <p style='text-align: right; color: #0066CC;'>This text is right-aligned</p>
            <p style='text-align: left; color: #0066CC;'>This text is left-aligned</p>

            <h2 style='color: #0066CC;'>Margins and Padding Demo</h2>
            <div style='background-color: #E0E0E0; padding: 20px; margin: 10px; color: #333333;'>
                <p>This box has 20px padding and 10px margin for spacing demonstration.</p>
            </div>

            <h2 style='color: #0066CC; margin-bottom: 5px;'>Footer</h2>
            <footer style='background-color: #333333; color: #FFFFFF; padding: 10px; text-align: center;'>
                Demo Complete - All supported HTML elements showcased! ✨
            </footer>
        ";
}
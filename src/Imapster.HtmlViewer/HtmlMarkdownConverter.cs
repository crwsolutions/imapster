using ReverseMarkdown;
using System.Diagnostics;
using System.Text;

namespace Imapster.HtmlViewer;

/// <summary>
/// Converts HTML email bodies to compact Markdown for AI classification input.
/// Applies a preprocessing pipeline (see
/// https://mysticmind.github.io/reversemarkdown-net/preprocessing) that strips
/// scripts, styles, hidden preheader text, tracking attributes and empty wrappers
/// before handing the document to ReverseMarkdown.
/// </summary>
public static class HtmlMarkdownConverter
{
    private static readonly Lazy<Config> _config = new(BuildConfig);

    /// <summary>
    /// Converts an HTML email body to Markdown. Returns an empty string for null or
    /// whitespace-only input. Emits a single <see cref="Debug.WriteLine"/> line with the
    /// UTF-8 byte count of the input and the converted output.
    /// </summary>
    public static string ToMarkdown(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var markdown = new Converter(_config.Value).Convert(html);

        var beforeBytes = Encoding.UTF8.GetByteCount(html);
        var afterBytes = Encoding.UTF8.GetByteCount(markdown);
        Debug.WriteLine($"HtmlToMarkdown: {beforeBytes} bytes HTML -> {afterBytes} bytes markdown");

        return markdown;
    }

    internal static Config BuildConfig()
    {
        var config = new Config
        {
            GithubFlavored = true,
            Tables =
            {
                // Flatten lists inside table cells to inline text so no raw HTML
                // survives in the output (recommended for LLM/RAG input).
                CellListHandling = Config.TableCellListHandlingOption.InlineText
            }
        };

        // Pipeline order matters. Steps that read the `style` attribute
        // (RemoveHidden, ConvertInlineStylesToTags) must run before RemoveStyles,
        // which strips every remaining style attribute.
        config.Preprocess
            .RemoveScripts()               // <script>, <noscript>, on* handlers
            .RemoveComments()              // HTML comments
            .RemoveHidden()                // display:none / visibility:hidden / hidden (preheader)
            .ConvertInlineStylesToTags()   // font-weight:700 -> <strong>, font-style:italic -> <em>, ...
            .RemoveStyles()                // remaining inline styles, <style>, stylesheet <link>
            .Unwrap("span, font")          // shed presentational wrappers
            .RemoveClasses(":not(pre):not(code)")
            .RemoveAttributes("*", "data-*", "id")
            .RemoveEmptyElements();

        return config;
    }
}

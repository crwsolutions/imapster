using Imapster.HtmlViewer;
using System.Text;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

/// <summary>
/// Tests for <see cref="HtmlMarkdownConverter.ToMarkdown"/>, proving the HTML to
/// Markdown conversion works and shrinks the payload for AI classification input.
/// </summary>
public class HtmlMarkdownConverterTests
{
    [Fact]
    public void ToMarkdown_ConvertsStrongAndLink()
    {
        var html = "This a sample <strong>paragraph</strong> from " +
                   "<a href=\"http://test.com\">my site</a>";

        var markdown = HtmlMarkdownConverter.ToMarkdown(html);

        Assert.Contains("**paragraph**", markdown);
        Assert.Contains("[my site](http://test.com)", markdown);
        Assert.DoesNotContain("<strong>", markdown);
        Assert.DoesNotContain("<a ", markdown);
    }

    [Fact]
    public void ToMarkdown_ConvertsUnorderedList()
    {
        var html = "<ul><li>One</li><li>Two</li></ul>";

        var markdown = HtmlMarkdownConverter.ToMarkdown(html);

        Assert.Contains("One", markdown);
        Assert.Contains("Two", markdown);
        Assert.DoesNotContain("<li>", markdown);
        Assert.DoesNotContain("<ul>", markdown);
    }

    [Fact]
    public void ToMarkdown_RemovesHiddenPreheader()
    {
        var html = "<span style=\"display:none\">secret preheader text</span><p>Visible</p>";

        var markdown = HtmlMarkdownConverter.ToMarkdown(html);

        Assert.Contains("Visible", markdown);
        Assert.DoesNotContain("secret preheader text", markdown);
    }

    [Fact]
    public void ToMarkdown_RemovesScriptsAndStyles()
    {
        var html = "<script>track(1)</script>" +
                   "<style>p { color: red; }</style>" +
                   "<p style=\"color:red\">Body</p>";

        var markdown = HtmlMarkdownConverter.ToMarkdown(html);

        Assert.Contains("Body", markdown);
        Assert.DoesNotContain("track(1)", markdown);
        Assert.DoesNotContain("<script>", markdown);
        Assert.DoesNotContain("color:red", markdown);
    }

    [Fact]
    public void ToMarkdown_PromotesInlineStylesToTags()
    {
        var html = "<span style=\"font-weight:700\">bold</span> and " +
                   "<span style=\"font-style:italic\">italic</span>";

        var markdown = HtmlMarkdownConverter.ToMarkdown(html);

        Assert.Contains("**bold**", markdown);
        Assert.Contains("*italic*", markdown);
        Assert.DoesNotContain("font-weight", markdown);
    }

    [Fact]
    public void ToMarkdown_ShedsPresentationalWrappers()
    {
        var html = "<div><span><font color=\"#000\">text</font></span></div>";

        var markdown = HtmlMarkdownConverter.ToMarkdown(html);

        Assert.Contains("text", markdown);
        Assert.DoesNotContain("color=\"#000\"", markdown);
        Assert.DoesNotContain("<font", markdown);
        Assert.DoesNotContain("<span", markdown);
    }

    [Fact]
    public void ToMarkdown_KeepsTableCellListReadable()
    {
        var html = "<table><tr><td>" +
                   "<ol><li>First point</li><li>Second point</li></ol>" +
                   "</td></tr></table>";

        var markdown = HtmlMarkdownConverter.ToMarkdown(html);

        Assert.Contains("First point", markdown);
        Assert.Contains("Second point", markdown);
        Assert.DoesNotContain("<ol", markdown);
        Assert.DoesNotContain("<li", markdown);
    }

    [Fact]
    public void ToMarkdown_StripsTrackingAttributes()
    {
        var html = "<p id=\"lead\" data-track=\"1\" class=\"intro\">Content</p>";

        var markdown = HtmlMarkdownConverter.ToMarkdown(html);

        Assert.Contains("Content", markdown);
        Assert.DoesNotContain("data-track", markdown);
        Assert.DoesNotContain("id=\"lead\"", markdown);
        Assert.DoesNotContain("class=", markdown);
    }

    [Fact]
    public void ToMarkdown_ShrinksLargeHtmlPayload()
    {
        var html = BuildLargeEmailHtml();

        var markdown = HtmlMarkdownConverter.ToMarkdown(html);

        var beforeBytes = Encoding.UTF8.GetByteCount(html);
        var afterBytes = Encoding.UTF8.GetByteCount(markdown);

        Assert.True(beforeBytes >= 4096, $"test fixture should be >= 4KB, was {beforeBytes}");
        Assert.True(afterBytes < beforeBytes,
            $"expected markdown ({afterBytes} bytes) to be smaller than html ({beforeBytes} bytes)");
        Assert.Contains("Real order confirmation", markdown);
    }

    [Fact]
    public void ToMarkdown_ReturnsEmptyForNullOrWhitespace()
    {
        Assert.Equal(string.Empty, HtmlMarkdownConverter.ToMarkdown(null!));
        Assert.Equal(string.Empty, HtmlMarkdownConverter.ToMarkdown("   "));
        Assert.Equal(string.Empty, HtmlMarkdownConverter.ToMarkdown(""));
    }

    private static string BuildLargeEmailHtml()
    {
        var noise = new StringBuilder();
        for (var i = 0; i < 40; i++)
        {
            noise.Append("<p id=\"p\" data-track=\"")
                 .Append(i)
                 .Append("\" class=\"promo\" style=\"color:#112233; font-family:Arial; margin:10px 0; line-height:1.5;\">")
                 .Append("Promotional filler line ")
                 .Append(i)
                 .Append(" about discounts, sales, events and limited time offers. ")
                 .Append("Shop now and save more with our exclusive bundle deals. </p>");
        }

        return """
            <html><head>
            <style>
            body { font-family: Arial, sans-serif; margin: 0; padding: 0; }
            .header { background-color: #333; color: #fff; padding: 20px; }
            .footer { font-size: 10px; color: #999; padding: 15px; }
            .promo { background-color: #ffe; border: 1px solid #ff0; padding: 10px; }
            </style>
            </head>
            <body>
            <span style="display:none; font-size:1px; color:transparent;">preheader spam text</span>
            <script>console.log('tracking');</script>
            <div class="header"><h1>Order confirmation</h1></div>
            __NOISE__
            <p>Real order confirmation for your purchase.</p>
            <div class="footer">You received this email because you signed up.</div>
            </body></html>
            """.Replace("__NOISE__", noise.ToString());
    }
}

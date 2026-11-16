using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

/// <summary>
/// Tests for the CSS length and color helpers the parser runs every inline style
/// declaration through.
/// </summary>
public class StringExtensionsTests
{
    [Theory]
    [InlineData("12px", 12)]
    [InlineData("1.5em", 24)]
    [InlineData("1.5rem", 24)]   // regression: 'em' used to match before 'rem'
    [InlineData("2rem", 32)]
    [InlineData("16", 16)]
    [InlineData("  -3px  ", -3)]
    [InlineData("50%", 0)]
    [InlineData("auto", 0)]
    [InlineData("", 0)]
    public void ParseLength_ReturnsPixelsForTheKnownUnits(string value, double expected)
    {
        Assert.Equal(expected, value.ParseLength());
    }

    [Fact]
    public void ParseLength_PtIsScaledToPixels()
    {
        Assert.Equal(15.996, "12pt".ParseLength(), 3);
    }

    [Theory]
    [InlineData("rgb(255,0,0)", "#FF0000")]
    [InlineData("rgb(1, 2, 3)", "#010203")]
    [InlineData("#f00", "#ff0000")]          // 3-digit shorthand is expanded
    [InlineData("#333333", "#333333")]
    [InlineData("#000000ff", "#000000ff")]   // 8-digit values pass through
    [InlineData("Red", "#FF0000")]           // legacy markup is not case-consistent
    [InlineData("GRAY", "#808080")]
    public void ConvertColorToHex_NormalizesToASingleShape(string value, string expected)
    {
        Assert.Equal(expected, value.ConvertColorToHex());
    }

    [Theory]
    [InlineData("rgb(300,0,0)")]   // out of range: must not throw
    [InlineData("rgb(999,999,999)")]
    [InlineData("#12345")]         // neither 3, 6 nor 8 digits
    [InlineData("#gg0000")]
    [InlineData("notacolor")]
    [InlineData("")]
    public void ConvertColorToHex_UnsupportedValuesBecomeNull(string value)
    {
        Assert.Null(value.ConvertColorToHex());
    }

    /// <summary>
    /// A single malformed declaration used to abort parsing of the whole document:
    /// the layout engine never got a tree, so the email body stayed blank.
    /// </summary>
    [Fact]
    public void Parse_OutOfRangeRgb_IsDroppedWithoutBreakingTheDocument()
    {
        // Arrange
        var htmlParser = new HtmlParser();
        var html = "<p style=\"color: rgb(300,0,0); font-size: 1.5rem;\">Hello world</p>";

        // Act
        var htmlRoot = htmlParser.Parse(html);

        // Assert
        var paragraph = htmlRoot.Children[0];
        Assert.Null(paragraph.Style.Color);
        Assert.Equal(24, paragraph.Style.FontSize);
        Assert.NotEmpty(paragraph.Children);
    }
}

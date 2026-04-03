namespace Imapster.HtmlViewer.Rendering;

/// <summary>
/// Represents the rendering state and configuration for the HTML viewer.
/// </summary>
public sealed class RenderContext
{
    /// <summary>
    /// Gets or sets the HTML content to render.
    /// </summary>
    public string? HtmlContent { get; set; }

    /// <summary>
    /// Gets or sets the available width for rendering.
    /// </summary>
    public double AvailableWidth { get; set; }

    /// <summary>
    /// Gets or sets the available height for rendering.
    /// </summary>
    public double AvailableHeight { get; set; }

    /// <summary>
    /// Gets or sets the default text color.
    /// </summary>
    public Color TextColor { get; set; } = Colors.Black;

    /// <summary>
    /// Gets or sets the link color.
    /// </summary>
    public Color LinkColor { get; set; } = Colors.Blue;

    /// <summary>
    /// Gets or sets the visited link color.
    /// </summary>
    public Color VisitedLinkColor { get; set; } = Colors.Purple;

    /// <summary>
    /// Gets or sets the selection color.
    /// </summary>
    public Color SelectionColor { get; set; } = Color.FromRgba(0, 120, 215, 128);

    /// <summary>
    /// Gets or sets the default font size.
    /// </summary>
    public double FontSize { get; set; } = 16;

    /// <summary>
    /// Gets or sets the default font family.
    /// </summary>
    public string FontFamily { get; set; } = "Arial";

    /// <summary>
    /// Gets or sets whether text selection is enabled.
    /// </summary>
    public bool IsSelectionEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether links are enabled.
    /// </summary>
    public bool IsLinksEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the current selection range.
    /// </summary>
    public (int Start, int End)? SelectionRange { get; set; }

    /// <summary>
    /// Gets or sets the scale factor for high DPI displays.
    /// </summary>
    public double ScaleFactor { get; set; } = 1.0;

    /// <summary>
    /// Creates a new instance of the RenderContext.
    /// </summary>
    public RenderContext()
    {
    }

    /// <summary>
    /// Creates a new instance of the RenderContext with specified dimensions.
    /// </summary>
    public RenderContext(double width, double height)
    {
        AvailableWidth = width;
        AvailableHeight = height;
    }
}
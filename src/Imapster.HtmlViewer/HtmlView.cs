using Microsoft.Maui.Controls;

namespace Imapster.HtmlViewer;

/// <summary>
/// HTML View control placeholder.
/// </summary>
public class HtmlView : ContentView
{
    /// <summary>
    /// Gets or sets the HTML content.
    /// </summary>
    public string HtmlContent { get; set; } = string.Empty;
}
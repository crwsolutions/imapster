using Imapster.HtmlRendering.Events;
using Imapster.HtmlRendering.Interaction;
using Imapster.HtmlRendering.Layout;
using Imapster.HtmlRendering.Parsing;
using Imapster.HtmlRendering.Rendering;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui;

namespace Imapster.Controls;

public class HtmlView : SkiaView
{
    private readonly HtmlRenderer _renderer;
    private readonly SelectionService _selectionService;
    private readonly LinkHandler _linkHandler;
    private readonly LayoutEngine _layoutEngine;
    private readonly ILogger<HtmlView>? _logger;

    private LayoutNode? _layoutRoot;
    private HtmlNode? _htmlRoot;
    private Size _lastRenderSize;
    private bool _isLayoutDirty;
    private bool _isRenderDirty;

    public static readonly BindableProperty HtmlBodyProperty =
        BindableProperty.Create(nameof(HtmlBody), typeof(string), typeof(HtmlView), null,
            propertyChanged: OnHtmlBodyChanged);

    public static readonly BindableProperty SelectedTextProperty =
        BindableProperty.Create(nameof(SelectedText), typeof(string), typeof(HtmlView), null);

    public event EventHandler<HtmlViewLinkClickedEventArgs>? LinkClicked;
    public event EventHandler<HtmlViewTextSelectedEventArgs>? TextSelected;
    public event EventHandler<HtmlViewSelectionChangedEventArgs>? SelectionChanged;

    public string? HtmlBody
    {
        get => (string?)GetValue(HtmlBodyProperty);
        set => SetValue(HtmlBodyProperty, value);
    }

    public string? SelectedText
    {
        get => (string?)GetValue(SelectedTextProperty);
        private set => SetValue(SelectedTextProperty, value);
    }

    public HtmlView()
    {
        var textMeasureService = new TextMeasureService();
        _renderer = new HtmlRenderer(textMeasureService);
        _selectionService = new SelectionService(textMeasureService);
        _linkHandler = new LinkHandler();
        _layoutEngine = new LayoutEngine(textMeasureService);

        TouchEffect = new TouchEffect();
        TouchEffect.TouchAction += OnTouchAction;

        IsOpaque = false;
        BackgroundColor = Colors.White;
    }

    public HtmlView(ILogger<HtmlView> logger) : this()
    {
        _logger = logger;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width > 0 && height > 0 && (width != _lastRenderSize.Width || height != _lastRenderSize.Height))
        {
            _lastRenderSize = new Size(width, height);
            _isLayoutDirty = true;
            InvalidateSurface();
        }
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);

        var canvas = e.Surface.Canvas;
        canvas.Clear(Colors.Transparent.ToSKColor());

        if (_layoutRoot is null)
            return;

        try
        {
            _renderer.Render(canvas, _layoutRoot, _lastRenderSize);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error rendering HTML");
        }
    }

    private void OnTouchAction(object? sender, TouchActionEventArgs e)
    {
        if (e.Type != TouchActionType.Pressed && e.Type != TouchActionType.Moved)
            return;

        var position = e.Location;

        if (_layoutRoot is null || _htmlRoot is null)
            return;

        var hitResult = HitTestEngine.HitTest(_layoutRoot, position.X, position.Y);

        if (hitResult is { Found: true, CharacterIndex: >= 0 } result)
        {
            _selectionService.HandleTouchAction(result, e.Type, position);

            if (_selectionService.SelectionStart >= 0 && _selectionService.SelectionEnd >= 0)
            {
                var selectedText = GetSelectedText();
                SelectedText = selectedText;

                TextSelected?.Invoke(this, new HtmlViewTextSelectedEventArgs(selectedText));
                SelectionChanged?.Invoke(this, new HtmlViewSelectionChangedEventArgs(
                    _selectionService.SelectionStart,
                    _selectionService.SelectionEnd
                ));
            }
            else
            {
                SelectedText = null;
            }

            InvalidateSurface();
        }
        else
        {
            _selectionService.HandleTouchAction(null, e.Type, position);
            InvalidateSurface();
        }
    }

    private string? GetSelectedText()
    {
        if (_htmlRoot is null || _selectionService.SelectionStart < 0 || _selectionService.SelectionEnd < 0)
            return null;

        var text = ExtractHtmlText(_htmlRoot);
        if (string.IsNullOrEmpty(text))
            return null;

        var start = _selectionService.SelectionStart;
        var end = _selectionService.SelectionEnd;

        if (start > text.Length || end > text.Length)
            return null;

        if (start > end)
            (start, end) = (end, start);

        return text.Substring(start, end - start);
    }

    private string ExtractHtmlText(HtmlNode node)
    {
        if (node.Type == HtmlElementType.Text)
            return node.Text ?? string.Empty;

        var text = string.Empty;
        foreach (var child in node.Children)
        {
            text += ExtractHtmlText(child);
        }

        return text;
    }

    private static void OnHtmlBodyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is HtmlView htmlView && newValue is string html)
        {
            htmlView._htmlRoot = HtmlParser.Parse(html);
            htmlView._isLayoutDirty = true;
            htmlView._isRenderDirty = true;

            if (htmlView._lastRenderSize.Width > 0 && htmlView._lastRenderSize.Height > 0)
            {
                htmlView._layoutRoot = htmlView._layoutEngine.Layout(htmlView._htmlRoot, htmlView._lastRenderSize);
                htmlView._isLayoutDirty = false;
                htmlView.InvalidateSurface();
            }
        }
    }

    public void ClearSelection()
    {
        _selectionService.ClearSelection();
        SelectedText = null;
        InvalidateSurface();
    }

    public void RefreshLayout()
    {
        if (_htmlRoot is not null && _lastRenderSize.Width > 0 && _lastRenderSize.Height > 0)
        {
            _layoutRoot = _layoutEngine.Layout(_htmlRoot, _lastRenderSize);
            _isLayoutDirty = false;
            InvalidateSurface();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderer.ClearCache();
            TouchEffect.TouchAction -= OnTouchAction;
        }

        base.Dispose(disposing);
    }
}
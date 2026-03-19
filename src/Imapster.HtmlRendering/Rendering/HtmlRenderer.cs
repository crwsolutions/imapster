namespace Imapster.HtmlRendering.Rendering;

using Imapster.HtmlRendering.Layout;
using SkiaSharp;

public class HtmlRenderer
{
    private readonly ITextMeasureService _textMeasureService;
    private readonly Dictionary<(string fontFamily, float fontSize), SKTypeface> _typefaceCache = [];
    private readonly Dictionary<string, SKBitmap> _imageCache = [];

    public HtmlRenderer(ITextMeasureService textMeasureService)
    {
        _textMeasureService = textMeasureService;
    }

    public void Render(SKCanvas canvas, LayoutNode root, Size renderSize)
    {
        if (root is null || root.Width <= 0 || root.Height <= 0)
            return;

        canvas.Save();
        canvas.ClipRect(new SKRect(0, 0, (float)renderSize.Width, (float)renderSize.Height));

        RenderNode(canvas, root, renderSize);

        canvas.Restore();
    }

    private void RenderNode(SKCanvas canvas, LayoutNode node, Size renderSize)
    {
        if (node is null)
            return;

        if (node.IsText)
        {
            RenderText(canvas, node);
        }
        else if (node.HasImage)
        {
            RenderImage(canvas, node);
        }
        else if (node.IsContainer)
        {
            RenderContainer(canvas, node, renderSize);
        }

        foreach (var child in node.Children)
        {
            RenderNode(canvas, child, renderSize);
        }
    }

    private void RenderText(SKCanvas canvas, LayoutNode node)
    {
        if (node is null || string.IsNullOrEmpty(node.Text))
            return;

        var paint = CreatePaint(node);
        var typeface = GetTypeface(node);
        var font = new SKFont(typeface, (float)node.FontSize);
        font.SubpixelText = true;
        font.Ligatures = true;

        var x = (float)node.X;
        var y = (float)(node.Y + node.FontSize);
        canvas.DrawText(node.Text, x, y, paint, font);

        paint.Dispose();
        font.Dispose();
    }

    private void RenderImage(SKCanvas canvas, LayoutNode node)
    {
        if (node is null || string.IsNullOrEmpty(node.ImageSrc))
            return;

        if (!_imageCache.TryGetValue(node.ImageSrc, out var bitmap))
        {
            bitmap = LoadImage(node.ImageSrc);
            if (bitmap is null)
                return;

            _imageCache[node.ImageSrc] = bitmap;
        }

        var drawRect = new SKRect(
            (float)node.X,
            (float)node.Y,
            (float)(node.X + node.Width),
            (float)(node.Y + node.Height)
        );

        var imageRect = new SKRect(
            0,
            0,
            bitmap.Width,
            bitmap.Height
        );

        canvas.DrawImage(bitmap, drawRect, imageRect);
    }

    private void RenderContainer(SKCanvas canvas, LayoutNode node, Size renderSize)
    {
        if (node.BackgroundColor is { } bgColor)
        {
            var paint = new SKPaint
            {
                Color = SKColor.Parse(bgColor),
                IsAntialiased = true
            };

            var rect = new SKRect((float)node.X, (float)node.Y, (float)(node.X + node.Width), (float)(node.Y + node.Height));
            canvas.DrawRect(rect, paint);
            paint.Dispose();
        }

        foreach (var child in node.Children)
        {
            RenderNode(canvas, child, renderSize);
        }
    }

    private SKPaint CreatePaint(LayoutNode node)
    {
        var paint = new SKPaint
        {
            Color = SKColor.Parse(node.Color ?? "#000000"),
            IsAntialiased = true
        };

        return paint;
    }

    private SKTypeface GetTypeface(LayoutNode node)
    {
        var key = (node.FontFamily ?? "Arial", (float)node.FontSize);

        if (_typefaceCache.TryGetValue(key, out var cached))
            return cached;

        var fontStyle = new SKFontStyle(
            node.IsBold ? SKFontWeight.Bold : SKFontWeight.Normal,
            SKFontStyleWidth.Normal,
            node.IsItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright
        );

        var typeface = SKTypeface.FromFamilyName(node.FontFamily ?? "Arial", fontStyle);
        _typefaceCache[key] = typeface;

        return typeface;
    }

    private SKBitmap? LoadImage(string source)
    {
        try
        {
            if (source.StartsWith("data:"))
            {
                var parts = source.Split(',');
                if (parts.Length < 2)
                    return null;

                var base64 = parts[1];
                var bytes = Convert.FromBase64String(base64);
                using var stream = new MemoryStream(bytes);
                return SKBitmap.Decode(stream);
            }
            else if (source.StartsWith("file://"))
            {
                var filePath = source[7..];
                if (File.Exists(filePath))
                {
                    using var stream = File.OpenRead(filePath);
                    return SKBitmap.Decode(stream);
                }
            }
        }
        catch
        {
            // Silently fail image loading
        }

        return null;
    }

    public void ClearCache()
    {
        foreach (var bitmap in _imageCache.Values)
        {
            bitmap.Dispose();
        }
        _imageCache.Clear();

        foreach (var typeface in _typefaceCache.Values)
        {
            typeface.Dispose();
        }
        _typefaceCache.Clear();
    }
}
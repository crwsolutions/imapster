using Imapster.ContentViews;
using System.Windows.Input;

namespace Imapster.ContentViews;

public partial class GlyphOnlyButton : Border
{
    public GlyphOnlyButton()
    {
        InitializeComponent();
    }

    public event EventHandler? Clicked;

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(ImapsterMaterialButton), null);

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(ImapsterMaterialButton), null);

    public static readonly BindableProperty RippleColorProperty =
        BindableProperty.Create(nameof(RippleColor), typeof(Color), typeof(ImapsterMaterialButton), Colors.White);

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public Color RippleColor
    {
        get => (Color)GetValue(RippleColorProperty);
        set => SetValue(RippleColorProperty, value);
    }

    public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
        nameof(BorderColor), typeof(Color), typeof(GlyphOnlyButton), default(Color));

    public Color BorderColor
    {
        get => (Color)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
        nameof(TextColor), typeof(Color), typeof(GlyphOnlyButton), default(Color));

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public static readonly BindableProperty ButtonColorProperty = BindableProperty.Create(
    nameof(ButtonColor), typeof(Color), typeof(GlyphOnlyButton), default(Color));

    public Color ButtonColor
    {
        get => (Color)GetValue(ButtonColorProperty);
        set => SetValue(ButtonColorProperty, value);
    }

    public static readonly BindableProperty GlyphProperty = BindableProperty.Create(
        nameof(Glyph), typeof(string), typeof(GlyphOnlyButton), default(string));

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(GlyphOnlyButton), default(string));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(
        nameof(FontFamily), typeof(string), typeof(GlyphOnlyButton), "FontAwesomeSolid", BindingMode.OneTime);

    public string FontFamily
    {
        get => (string)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize), typeof(double), typeof(GlyphOnlyButton), 16.0, BindingMode.OneTime);

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public static readonly BindableProperty FontAttributesProperty = BindableProperty.Create(
        nameof(FontAttributes), typeof(FontAttributes), typeof(GlyphOnlyButton), FontAttributes.None, BindingMode.OneTime);

    public FontAttributes FontAttributes
    {
        get => (FontAttributes)GetValue(FontAttributesProperty);
        set => SetValue(FontAttributesProperty, value);
    }

    private void MaterialButton_Clicked(object? sender, EventArgs e)
    {
        Clicked?.Invoke(this, EventArgs.Empty);
    }
}

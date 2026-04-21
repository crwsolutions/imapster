using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Imapster.ContentViews;

public partial class GlyphMaterialButton : Border
{
    public GlyphMaterialButton()
    {
        InitializeComponent();

        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(6, 6, 6, 6) };
    }

    public event EventHandler? Clicked;

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(GlyphMaterialButton), null);

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(GlyphMaterialButton), null, BindingMode.TwoWay );

    public static readonly BindableProperty RippleColorProperty =
        BindableProperty.Create(nameof(RippleColor), typeof(Color), typeof(GlyphMaterialButton), Colors.White);

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
        nameof(BorderColor), typeof(Color), typeof(GlyphMaterialButton), default(Color));

    public Color BorderColor
    {
        get => (Color)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
        nameof(TextColor), typeof(Color), typeof(GlyphMaterialButton), default(Color));

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public static readonly BindableProperty ButtonColorProperty = BindableProperty.Create(
    nameof(ButtonColor), typeof(Color), typeof(GlyphMaterialButton), default(Color));

    public Color ButtonColor
    {
        get => (Color)GetValue(ButtonColorProperty);
        set => SetValue(ButtonColorProperty, value);
    }

    public static readonly BindableProperty GlyphProperty = BindableProperty.Create(
        nameof(Glyph), typeof(string), typeof(GlyphMaterialButton), default(string));

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(
    nameof(FontFamily), typeof(string), typeof(GlyphOnlyButton), "FontAwesomeSolid", BindingMode.OneTime);

    public string FontFamily
    {
        get => (string)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(GlyphMaterialButton), default(string));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty ShowForwardNavigationIconProperty = BindableProperty.Create(
        nameof(ShowForwardNavigationIcon), typeof(bool), typeof(GlyphMaterialButton), false);

    public bool ShowForwardNavigationIcon
    {
        get => (bool)GetValue(ShowForwardNavigationIconProperty);
        set => SetValue(ShowForwardNavigationIconProperty, value);
    }

    private void MaterialButton_Clicked(object? sender, EventArgs e)
    {
        if (Command != null && Command.CanExecute(CommandParameter))
        {
            Command.Execute(CommandParameter);
        }
        Clicked?.Invoke(this, e);
    }
}

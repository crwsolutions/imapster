using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Imapster.ContentViews;

[ContentProperty(nameof(MyContent))]
public partial class ImapsterMaterialButton : ContentView
{
    private readonly Border _rippleEffect;
    private readonly Grid _mainContainer;

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

    public View MyContent
    {
        get => (View)_mainContainer.Children[0]; // Get user content
        set => _mainContainer.Children.Insert(0, value); // Add new content
    }

    public event EventHandler? Clicked;

    public ImapsterMaterialButton()
    {
        _rippleEffect = new Border
        {
            BackgroundColor = Colors.Transparent,
            StrokeThickness = 0,
            Opacity = 0,
            IsVisible = false,
            StrokeShape = new RoundRectangle { CornerRadius = 50 }
        };

        _mainContainer = new Grid
        {
            BackgroundColor = Colors.Transparent
        };

        _mainContainer.Children.Add(_rippleEffect); // Add ripple effect (always at index 1)

        var tapGestureRecognizer = new TapGestureRecognizer();
        tapGestureRecognizer.Tapped += async (s, e) =>
        {
            await AnimateRipple(e.GetPosition(this));
            Command?.Execute(CommandParameter);
            Clicked?.Invoke(this, EventArgs.Empty);
        };

        GestureRecognizers.Add(tapGestureRecognizer);

        Content = _mainContainer; // Set the container as the actual ContentView content
    }

    private async Task AnimateRipple(Point? touchPoint)
    {
        if (touchPoint is null)
            return;

        _rippleEffect.IsVisible = true;
        _rippleEffect.BackgroundColor = RippleColor;
        _rippleEffect.Opacity = 0.3;
        _rippleEffect.Scale = 1; // start small

        const int rippleSize = 10;
        _rippleEffect.WidthRequest = rippleSize;
        _rippleEffect.HeightRequest = rippleSize;

        double maxSize = Math.Max(Width, Height) * 1.5;

        // Center ripple at touch point
        var adjustedX = touchPoint.Value.X - (rippleSize / 2);
        var adjustedY = touchPoint.Value.Y - (rippleSize / 2);

        _rippleEffect.TranslationX = adjustedX - (Width / 2);
        _rippleEffect.TranslationY = adjustedY - (Height / 2);

        const uint fadeDuration = 220;
        const uint scaleDuration = 300;

        await Task.WhenAny(
            _rippleEffect.ScaleToAsync(maxSize / rippleSize, scaleDuration, Easing.SinOut),
            _rippleEffect.FadeToAsync(0, fadeDuration, Easing.SinIn)
        );

        _rippleEffect.IsVisible = false;
        _rippleEffect.Opacity = 0;
        _rippleEffect.Scale = 1;
    }
}

using Microsoft.Maui.Controls.Shapes;

namespace Imapster.ContentViews;

public partial class ToggleSwitchView : ContentView
{
    public event EventHandler<bool>? Toggled;

    public ToggleSwitchView()
    {
        InitializeComponent();
        UpdateUI(false, animate: false);
    }

    #region Bindable Properties

    public static readonly BindableProperty LeftTextProperty =
        BindableProperty.Create(
            nameof(LeftText),
            typeof(string),
            typeof(ToggleSwitchView),
            "ON",
            propertyChanged: (b, o, n) =>
                ((ToggleSwitchView)b).LeftLabel.Text = (string)n);

    public static readonly BindableProperty RightTextProperty =
        BindableProperty.Create(
            nameof(RightText),
            typeof(string),
            typeof(ToggleSwitchView),
            "OFF",
            propertyChanged: (b, o, n) =>
                ((ToggleSwitchView)b).RightLabel.Text = (string)n);

    public static readonly BindableProperty IsRightSelectedProperty =
        BindableProperty.Create(
            nameof(IsRightSelected),
            typeof(bool),
            typeof(ToggleSwitchView),
            false,
            propertyChanged: (b, o, n) =>
                ((ToggleSwitchView)b).UpdateUI((bool)n, true));

    public string LeftText
    {
        get => (string)GetValue(LeftTextProperty);
        set => SetValue(LeftTextProperty, value);
    }

    public string RightText
    {
        get => (string)GetValue(RightTextProperty);
        set => SetValue(RightTextProperty, value);
    }

    public bool IsRightSelected
    {
        get => (bool)GetValue(IsRightSelectedProperty);
        set => SetValue(IsRightSelectedProperty, value);
    }

    #endregion

    private double _columnWidth;
    private double _sliderWidth = 75;
    private double _sliderHeight = 30;

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width <= 0 || height <= 0)
            return;

        // Calculate the width of each column (accounting for padding)
        double contentWidth = width - RootGrid.Padding.HorizontalThickness;
        _columnWidth = contentWidth / 2;

        // Set slider size - pill shape around the text
        Slider.WidthRequest = _sliderWidth;
        Slider.HeightRequest = _sliderHeight;

        // Apply rounded corners to the grid background
        RootGrid.Clip = new RoundRectangleGeometry
        {
            CornerRadius = height / 3,
            Rect = new Rect(0, 0, width, height)
        };

        // Apply rounded corners to the slider (pill shape)
        Slider.StrokeShape = new RoundRectangle
        {
            CornerRadius = _sliderHeight / 3
        };

        UpdateUI(IsRightSelected, animate: false);
    }

    private void UpdateUI(bool isRight, bool animate)
    {
        // Slider starts centered in left column (Grid.Column="0" with HorizontalOptions="Center")
        // To position in left column: TranslationX = 0 (stay in place)
        // To position in right column: TranslationX = _columnWidth (move to right column center)
        double targetX = isRight ? _columnWidth : 0;

        if (animate)
        {
            _ = Slider.TranslateToAsync(targetX, 0, 200, Easing.CubicOut);
        }
        else
        {
            Slider.TranslationX = targetX;
        }

        LeftLabel.TextColor = isRight ? Colors.Gray : Colors.Black;
        RightLabel.TextColor = isRight ? Colors.Black : Colors.Gray;
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        var pos = e.GetPosition(RootGrid);
        if (pos == null)
            return;

        bool newValue = pos.Value.X > RootGrid.Width / 2;

        if (newValue != IsRightSelected)
        {
            IsRightSelected = newValue;
            Toggled?.Invoke(this, newValue);
        }
    }
}

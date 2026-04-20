namespace Imapster.ContentViews;

public partial class SplitView : Grid
{
    private double initialLeftWidth;

    public SplitView()
    {
        InitializeComponent();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
    }

    // ==================== BINDABLE PROPERTIES ====================

    public static readonly BindableProperty LeftContentProperty =
        BindableProperty.Create(nameof(LeftContent), typeof(View), typeof(SplitView),
            null, propertyChanged: OnLeftContentChanged);

    public static readonly BindableProperty RightContentProperty =
        BindableProperty.Create(nameof(RightContent), typeof(View), typeof(SplitView),
            null, propertyChanged: OnRightContentChanged);

    public View LeftContent
    {
        get => (View)GetValue(LeftContentProperty);
        set => SetValue(LeftContentProperty, value);
    }

    public View RightContent
    {
        get => (View)GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    private static void OnLeftContentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SplitView control && newValue is View content)
        {
            control.LeftPanel.Content = content;
        }
    }

    private static void OnRightContentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SplitView control && newValue is View content)
        {
            control.RightPanel.Content = content;
        }
    }

    // ==================== SPLITTER ====================
    private void OnSplitterPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (e.StatusType == GestureStatus.Started)
        {
            initialLeftWidth = LeftPanel.Width;
        }
        else if (e.StatusType == GestureStatus.Running)
        {
            double newWidth = Math.Max(100, initialLeftWidth + e.TotalX);
            newWidth = Math.Min(Width - 100, newWidth);

            ColumnDefinitions[0].Width = new GridLength(newWidth, GridUnitType.Absolute);
            InvalidateMeasure();
        }
        else if (e.StatusType == GestureStatus.Completed)
        {
            initialLeftWidth = LeftPanel.Width;
        }
    }
}

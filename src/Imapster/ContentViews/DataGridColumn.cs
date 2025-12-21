namespace Imapster.ContentViews;

[DebuggerDisplay("Column: {Key} {Width}")]
public class DataGridColumn : BindableObject
{
    public string Key { get; set; } = default!;
    public string Header { get; set; } = default!;
    public bool IsSortable { get; set; } = true;
    public bool IsFilterable { get; set; } = true;

    public int? Width { get; set; }

    public bool IsVisible
    {
        get => (bool)GetValue(IsVisibleProperty);
        set => SetValue(IsVisibleProperty, value);
    }
    public static readonly BindableProperty IsVisibleProperty =
        BindableProperty.Create(nameof(IsVisible), typeof(bool), typeof(DataGridColumn), true);

    public DataTemplate? ContentTemplate { get; set; }
}
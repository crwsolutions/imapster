using System.Collections;
using System.Windows.Input;

namespace Imapster.ContentViews;

public partial class TreeView : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(TreeView), null);

    public static readonly BindableProperty SelectedItemProperty =
        BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(TreeView), null, BindingMode.TwoWay);

    public static readonly BindableProperty ItemTemplateProperty =
        BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(TreeView), null);

    public static readonly BindableProperty EmptyTrashCommandProperty =
        BindableProperty.Create(nameof(EmptyTrashCommand), typeof(ICommand), typeof(TreeView), null);

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public DataTemplate ItemTemplate
    {
        get => (DataTemplate)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public ICommand EmptyTrashCommand
    {
        get => (ICommand)GetValue(EmptyTrashCommandProperty);
        set => SetValue(EmptyTrashCommandProperty, value);
    }

    public TreeView()
    {
        InitializeComponent();
    }

    public void OnItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Grid grid && grid.BindingContext is FolderViewModel treeFolder)
        {
            SelectedItem = treeFolder;
        }
    }
}
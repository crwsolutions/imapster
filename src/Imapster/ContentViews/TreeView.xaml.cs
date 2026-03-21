using System.Collections;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls;

namespace Imapster.ContentViews;

public partial class TreeView : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(TreeView), null);

    public static readonly BindableProperty SelectedItemProperty =
        BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(TreeView), null,
            propertyChanged: OnSelectedItemChanged);

    public static readonly BindableProperty SelectionModeProperty =
        BindableProperty.Create(nameof(SelectionMode), typeof(SelectionMode), typeof(TreeView), SelectionMode.Single);

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

    public SelectionMode SelectionMode
    {
        get => (SelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
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
        var collectionView = this.FindByName<CollectionView>("FolderCollection");
        collectionView?.SelectionChanged += OnCollectionViewSelectionChanged;
    }

    private static void OnSelectedItemChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TreeView treeView)
        {
            var collectionView = treeView.FindByName<CollectionView>("FolderCollection");
            if (collectionView != null && newValue != null)
            {
                collectionView.SelectedItem = newValue;
            }
        }
    }

    private void OnCollectionViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView collectionView)
        {
            SelectedItem = collectionView.SelectedItem;
        }
    }

    public void OnItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Grid grid && grid.BindingContext is FolderViewModel treeFolder)
        {
            SelectedItem = treeFolder;
        }
    }
}
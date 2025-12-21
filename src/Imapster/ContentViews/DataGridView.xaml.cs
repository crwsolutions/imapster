using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Extensions;
using Imapster.ContentViews.Popups;
using Imapster.Converters;
using System.Buffers;
using System.Collections;
using System.Collections.Specialized;
using System.Text.Json;

namespace Imapster.ContentViews
{
    public partial class DataGridView : ContentView
    {
        private string? _currentSortKey;
        private bool _sortAscending = true;
        private Dictionary<string, List<object?>> _filters = new();
        private ObservableCollection<IDataGridItem> _displayedItems = new ObservableCollection<IDataGridItem>();

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(DataGridView), default, BindingMode.TwoWay,
                propertyChanged: OnItemsSourceChanged);

        public IList<DataGridColumn> Columns
        {
            get => (IList<DataGridColumn>)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public static readonly BindableProperty ColumnsProperty =
            BindableProperty.Create(nameof(Columns), typeof(IList<DataGridColumn>), typeof(DataGridView), new List<DataGridColumn>(), BindingMode.OneTime);

        public IList<object> SelectedItems { get; } = new List<object>();

        public string PreferencesKey { get; set; } = "DefaultDataGrid";

        public ObservableCollection<IDataGridItem> DisplayedItems => _displayedItems;

        public int Count
        {
            get => (int)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }

        public static readonly BindableProperty CountProperty =
            BindableProperty.Create(nameof(Count), typeof(int), typeof(DataGridView), default(int), BindingMode.TwoWay);

        public DataGridView() => InitializeComponent();

        private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (oldValue is INotifyCollectionChanged observableObject)
            {
                observableObject.CollectionChanged -= (s, e) => { };
            }
            var control = (DataGridView)bindable;
            if (newValue is INotifyCollectionChanged observableCollection)
            {
                observableCollection.CollectionChanged += (s, e) =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems is not null)
                    { 
                        foreach (var item in e.OldItems)
                        {
                            if (item is IDataGridItem dataGridItem)
                            {
                                control.RemoveItem(dataGridItem);
                            }
                        }
                    }
                };
            }

            control.RefreshData();
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            RestoreState();
        }

        public void RemoveItem(IDataGridItem item)
        {
            _displayedItems.Remove(item);
        }

        private void RefreshData()
        {
            if (ItemsSource is not IEnumerable source)
                return;

            var items = source.Cast<IDataGridItem>().ToList();

            // Apply filters
            var filteredItems = ApplyFilters(items);

            // Apply sorting
            var sortedItems = ApplySorting(filteredItems);

            _displayedItems = sortedItems.ToObservableCollection();
            
            // Set the Count property
            Count = _displayedItems.Count;

            // Rebuild the UI
            RebuildHeaders();
            RebuildRows();
        }

        private IEnumerable<IDataGridItem> ApplyFilters(IEnumerable<IDataGridItem> filteredItems)
        {
            foreach (var filter in _filters.Where(f => f.Value?.Count > 0))
            {
                var columnKey = filter.Key;
                var column = Columns.FirstOrDefault(c => c.Key == columnKey);
                if (column == null)
                    continue;

                // Create SearchValues<string> once per filter
                var stringArray = filter.Value
                    .Where(v => v is not null)
                    .Select(v => v!.ToString())
                    .ToArray();

                var searchValues = SearchValues.Create(stringArray.AsSpan()!, StringComparison.OrdinalIgnoreCase);

                filteredItems = filteredItems
                    .Where(item =>
                    {
                        var value = item.GetValue(columnKey)?.ToString();
                        return value is not null && searchValues.Contains(value);
                    });
            }

            return filteredItems;
        }

        private IEnumerable<IDataGridItem> ApplySorting(IEnumerable<IDataGridItem> items)
        {
            if (string.IsNullOrEmpty(_currentSortKey))
                return items;

            var column = Columns.FirstOrDefault(c => c.Key == _currentSortKey);
            if (column == null || !column.IsSortable)
                return items;

            var sortedItems = items.ToList();

            if (_sortAscending)
            {
                return sortedItems.OrderBy(item => item.GetValue(_currentSortKey));
            }
            else
            {
                return sortedItems.OrderByDescending(item => item.GetValue(_currentSortKey));
            }
        }

        private void RebuildHeaders()
        {
            HeaderGrid.Children.Clear();
            HeaderGrid.ColumnDefinitions.Clear();

            // Calculate visible columns
            var visibleColumns = Columns.Where(c => c.IsVisible).ToList();

            // Set column definitions
            foreach (var column in visibleColumns)
            {
                var width = column.Width.HasValue
                    ? new GridLength(column.Width.Value, GridUnitType.Absolute)
                    : new GridLength(1, GridUnitType.Star);

                HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = width });
            }

            // Create header cells
            for (int i = 0; i < visibleColumns.Count; i++)
            {
                var column = visibleColumns[i];

                var headerGrid = new Grid()
                {
                    ColumnDefinitions = [new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) }]
                };
                if (column.IsSortable)
                {
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                }
                if (column.IsFilterable)
                {
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                }
                var buttonCounter = 0;
                // Header text
                var headerLabel = new Label()
                {
                    Text = column.Header,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Start,
                    LineBreakMode = LineBreakMode.TailTruncation,
                    Margin = new Thickness(8, 8, 0, 8),
                };
                headerGrid.Add(headerLabel, buttonCounter++);

                // Sort button (ascending)
                if (column.IsSortable)
                {
                    var sortAscButton = new GlyphOnlyButton()
                    {
                        Glyph = "\uf0dd",
                        VerticalOptions = LayoutOptions.Center,
                        TextColor = Colors.White,
                        FontSize = 12,
                        WidthRequest = 28,
                        HeightRequest = 28,
                        BackgroundColor = Colors.Red
                    };
                    sortAscButton.Clicked += OnSortAscendingClicked;
                    sortAscButton.BindingContext = column;

                    // Sort button (descending)
                    var sortDescButton = new GlyphOnlyButton()
                    {
                        Glyph = "\uf0de",
                        VerticalOptions = LayoutOptions.Center,
                        TextColor = Colors.White,
                        FontSize = 12,
                        WidthRequest = 28,
                        HeightRequest = 28,
                        BackgroundColor = Colors.Blue
                    };
                    sortDescButton.Clicked += OnSortDescendingClicked;
                    sortDescButton.BindingContext = column;

                    headerGrid.Add(sortAscButton, buttonCounter++);
                    headerGrid.Add(sortDescButton, buttonCounter++);
                }

                if (column.IsFilterable)
                {
                    // Filter button
                    var filterButton = new GlyphOnlyButton()
                    {
                        Glyph = "\uf0b0",
                        VerticalOptions = LayoutOptions.Center,
                        TextColor = Colors.White,
                        FontSize = 12,
                        WidthRequest = 28,
                        HeightRequest = 28,
                        BackgroundColor = Colors.Green
                    };
                    filterButton.Clicked += OnFilterClicked;
                    filterButton.BindingContext = column;
                    headerGrid.Add(filterButton, buttonCounter++);
                }

                // Reorder button
                var reorderButton = new GlyphOnlyButton()
                {
                    Glyph = "⋮",
                    FontSize = 12,
                    WidthRequest = 20,
                    HeightRequest = 20,
                    BackgroundColor = Colors.Purple
                };
                reorderButton.Clicked += OnReorderClicked;
                reorderButton.BindingContext = column;

                Grid.SetColumn(headerGrid, i);
                HeaderGrid.Children.Add(headerGrid);
            }
        }

        private void RebuildRows()
        {
            // Clear existing rows
            DataCollectionView.ItemsSource = null;

            // Create a new DataTemplate for the rows
            var dataTemplate = new DataTemplate(() =>
            {
                // Calculate visible columns
                var visibleColumns = Columns.Where(c => c.IsVisible).ToList();

                // Create the row Grid with proper column definitions
                var rowGrid = new Grid();

                // Set column definitions using the same logic as in RebuildHeaders
                foreach (var column in visibleColumns)
                {
                    var width = column.Width.HasValue
                        ? new GridLength(column.Width.Value, GridUnitType.Absolute)
                        : new GridLength(1, GridUnitType.Star);

                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = width });
                }
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // For details button

                // Create cells for each visible column
                for (int i = 0; i < visibleColumns.Count; i++)
                {
                    var column = visibleColumns[i];

                    // Check if a custom template is provided
                    if (column.ContentTemplate != null)
                    {
                        // Use the custom template
                        var content = column.ContentTemplate.CreateContent();
                        if (content is View view)
                        {
                            view.SetBinding(BindableObject.BindingContextProperty, new Binding(".", BindingMode.OneTime));
                            Grid.SetColumn(view, i);
                            rowGrid.Children.Add(view);
                        }
                    }
                    else
                    {
                        // Fall back to default behavior
                        // Create a label for the cell content
                        var cellLabel = new Label()
                        {
                            VerticalOptions = LayoutOptions.Center,
                            HorizontalOptions = LayoutOptions.Start,
                            LineBreakMode = LineBreakMode.TailTruncation,
                            Padding = new Thickness(5, 0)
                        };

                        // Bind the label text to the column's value selector
                        cellLabel.SetBinding(Label.TextProperty, new Binding
                        {
                            Path = column.Key
                        });

                        // Set the grid column
                        Grid.SetColumn(cellLabel, i);
                        rowGrid.Children.Add(cellLabel);
                    }
                }

                // Add tap gesture for multi-select
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += OnRowTapped;
                rowGrid.GestureRecognizers.Add(tapGesture);
                rowGrid.SetBinding(Grid.BackgroundProperty, new Binding
                {
                    Path = "IsSelected",
                    Converter = new BoolToColorConverter()
                });
                return rowGrid;
            });

            // Set the new template and items source
            DataCollectionView.ItemTemplate = dataTemplate;
            DataCollectionView.ItemsSource = _displayedItems;
        }

        private void OnSortAscendingClicked(object? sender, EventArgs e)
        {
            var column = (DataGridColumn)((BindableObject)sender!).BindingContext;
            ApplySort(column, ascending: true);
        }

        private void OnSortDescendingClicked(object? sender, EventArgs e)
        {
            var column = (DataGridColumn)((BindableObject)sender!).BindingContext;
            ApplySort(column, ascending: false);
        }

        private void ApplySort(DataGridColumn column, bool ascending)
        {
            _currentSortKey = column.Key;
            _sortAscending = ascending;
            RefreshData();
            SaveState();
        }

        private async void OnFilterClicked(object? sender, EventArgs e)
        {
            var column = (DataGridColumn)((BindableObject)sender!).BindingContext;
            var values = GetDistinctValues(column);

            var popup = new ColumnFilterPopup(column, values);
            var result = await Shell.Current.ShowPopupAsync<object>(popup);

            if (result.Result is List<object?> selectedValues)
            {
                ApplyFilter(column, selectedValues);
            }
        }

        private void ApplyFilter(DataGridColumn column, List<object?> selectedValues)
        {
            if (column.Key is null)
            {
                return;
            }

            if (selectedValues?.Count > 0)
            {
                _filters[column.Key] = selectedValues;
            }
            else
            {
                _filters.Remove(column.Key);
            }

            RefreshData();
            SaveState();
        }

        private List<object?> GetDistinctValues(DataGridColumn column) =>
            ItemsSource?.Cast<IDataGridItem>()
                .Select(item => item.GetValue(column.Key))
                .Distinct()
                .ToList() ?? [];

        private async void OnReorderClicked(object? sender, EventArgs e)
        {
            var popup = new ColumnReorderPopup(Columns);
            var newOrder = await Shell.Current.ShowPopupAsync(popup);

            if (newOrder is IList<DataGridColumn> reordered)
            {
                Columns = reordered;
                RebuildHeaders();
                SaveState();
            }
        }

        private async void OnColumnVisibilityClicked(object? sender, EventArgs e)
        {
            var popup = new ColumnVisibilityPopup(Columns);
            await Shell.Current.ShowPopupAsync(popup);
            RebuildHeaders();
            RebuildRows();
            SaveState();
        }

        private void SaveState()
        {
            var state = new DataGridState
            {
                ColumnOrder = Columns.Select(c => c.Key).ToList(),
                ColumnVisibility = Columns.ToDictionary(c => c.Key, c => c.IsVisible),
                SortColumn = _currentSortKey,
                SortAscending = _sortAscending,
                Filters = _filters
            };

            Preferences.Set(PreferencesKey, JsonSerializer.Serialize(state));
        }

        private void RestoreState()
        {
            if (!Preferences.ContainsKey(PreferencesKey))
                return;

            var stateJson = Preferences.Get(PreferencesKey, string.Empty);
            try
            {
                var state = JsonSerializer.Deserialize<DataGridState>(stateJson);
                if (state is not null)
                { 
                    ApplyState(state);
                }
            }
            catch (Exception)
            {
                // If deserialization fails, use defaults
            }
        }

        private void ApplyState(DataGridState state)
        {
            if (state == null) return;

            // Apply column visibility
            foreach (var col in Columns)
            {
                if (state.ColumnVisibility.TryGetValue(col.Key, out bool isVisible))
                {
                    col.IsVisible = isVisible;
                }
            }

            // Apply sorting
            _currentSortKey = state.SortColumn;
            _sortAscending = state.SortAscending;

            // Apply filters
            _filters = state.Filters ?? new Dictionary<string, List<object?>>();

            RefreshData();
        }

        // Multi-select functionality
        private void OnRowTapped(object? sender, TappedEventArgs e)
        {
            if (sender is BindableObject bo && bo.BindingContext is IDataGridItem rowItem)
            {
                rowItem.IsSelected = !rowItem.IsSelected;
                return;
            }
        }

        private void OnClearFilterTapped(object? sender, EventArgs e)
        {
            _filters.Clear();

            RefreshData();
            SaveState();
        }

        private void OnClearSortTapped(object? sender, EventArgs e)
        {
            _currentSortKey = null;

            RefreshData();
            SaveState();
        }

        private void OnClearSelectionTapped(object? sender, EventArgs e)
        {
            foreach (var item in _displayedItems)
            {   
                item.IsSelected = false;
            }
        }

        private void OnSelectAllTapped(object? sender, EventArgs e)
        {
            foreach (var item in _displayedItems)
            {
                item.IsSelected = true;
            }
        }
    }
}
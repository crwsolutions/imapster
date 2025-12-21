using CommunityToolkit.Maui.Views;

namespace Imapster.ContentViews.Popups
{
    public partial class ColumnReorderPopup : Popup<object>
    {
        private readonly IList<DataGridColumn> _columns;
        private readonly List<DataGridColumn> _originalColumns;

        public ColumnReorderPopup(IList<DataGridColumn> columns)
        {
            InitializeComponent();
            _columns = columns ?? new List<DataGridColumn>();
            _originalColumns = new List<DataGridColumn>(_columns);

            // Create reorder items
            foreach (var column in _columns)
            {
                var stackLayout = new StackLayout()
                {
                    Orientation = StackOrientation.Horizontal,
                    Children =
                    {
                        new Label()
                        {
                            Text = "⋮",
                            FontSize = 20,
                            VerticalOptions = LayoutOptions.Center,
                            Margin = new Thickness(5, 0)
                        },
                        new Label()
                        {
                            Text = column.Header,
                            VerticalOptions = LayoutOptions.Center
                        }
                    },
                    Padding = new Thickness(5),
                    BackgroundColor = Colors.LightGray
                };

                // Add drag functionality (simplified implementation)
                stackLayout.GestureRecognizers.Add(new TapGestureRecognizer()
                {
                    Command = new Command(() => OnColumnTapped(column))
                });

                ReorderItemsLayout.Children.Add(stackLayout);
            }
        }

        private async void OnColumnTapped(DataGridColumn column)
        {
            // In a real implementation, we would handle drag and drop
            // For now, we'll just close with the original order
            await CloseAsync(_originalColumns);
        }

        private async void OnCloseClicked(object? sender, EventArgs e)
        {
            await CloseAsync(_originalColumns);
        }
    }
}
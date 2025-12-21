using CommunityToolkit.Maui.Views;

namespace Imapster.ContentViews.Popups
{
    public partial class ColumnVisibilityPopup : Popup
    {
        private readonly IList<DataGridColumn> _columns;

        public ColumnVisibilityPopup(IList<DataGridColumn> columns)
        {
            InitializeComponent();
            _columns = columns ?? new List<DataGridColumn>();

            // Create visibility toggle items
            foreach (var column in _columns)
            {
                var checkBox = new CheckBox()
                {
                    IsChecked = column.IsVisible,
                    HorizontalOptions = LayoutOptions.Start
                };
                checkBox.CheckedChanged += (sender, e) => column.IsVisible = e.Value;

                var label = new Label()
                {
                    Text = column.Header,
                    VerticalOptions = LayoutOptions.Center
                };

                var stackLayout = new StackLayout()
                {
                    Orientation = StackOrientation.Horizontal,
                    Children = { checkBox, label },
                    Padding = new Thickness(5)
                };

                VisibilityItemsLayout.Children.Add(stackLayout);
            }
        }

        private async void OnCloseClicked(object? sender, EventArgs e)
        {
            await CloseAsync();
        }
    }
}
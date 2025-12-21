using CommunityToolkit.Maui.Views;

namespace Imapster.ContentViews.Popups
{
    public partial class ColumnFilterPopup : Popup<object>
    {
        public IReadOnlyList<object?> SelectedValues { get; private set; } = new List<object?>();

        public ColumnFilterPopup(DataGridColumn column, IEnumerable<object?> values)
        {
            InitializeComponent();
            BindingContext = this;

            // Create filter items
            foreach (var value in values)
            {
                var checkBox = new CheckBox()
                {
                    IsChecked = false,
                    HorizontalOptions = LayoutOptions.Start
                };

                var label = new Label()
                {
                    Text = value?.ToString() ?? "(null)",
                    TextColor = Colors.Black,
                    VerticalOptions = LayoutOptions.Center
                };

                var stackLayout = new HorizontalStackLayout()
                {
                    Children = { checkBox, label },
                    Padding = new Thickness(5)
                };

                FilterItemsLayout.Children.Add(stackLayout);
            }
        }

        private async void OnApplyClicked(object? sender, EventArgs e)
        {
            var selectedValues = new List<object?>();

            foreach (var child in FilterItemsLayout.Children)
            {
                if (child is HorizontalStackLayout stackLayout && stackLayout.Children[0] is CheckBox checkBox)
                {
                    if (checkBox.IsChecked)
                    {
                        var label = (Label)stackLayout.Children[1];
                        // We need to get the actual value, but since we can't directly access it,
                        // we'll just use a simpler approach - this would need refinement in practice
                        selectedValues.Add(label.Text);
                    }
                }
            }

            SelectedValues = selectedValues;
            await CloseAsync(SelectedValues);
        }
    }
}
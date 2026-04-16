using CommunityToolkit.Maui.Views;

namespace Imapster.Popups
{
    public partial class EmailDetailsPopup : Popup
    {
        public EmailDetailsPopup(EmailDetailsViewModel viewModel)
        {
            BindingContext = viewModel;
            InitializeComponent();
            
            // Initialize to Html tab (default)
            ShowTab("html");
        }

        private async void OnCloseClicked(object? sender, EventArgs e) => await CloseAsync();

        private async void OnEditPromptClicked(object? sender, EventArgs e)
        {
            var popupService = App.Services.GetRequiredService<IPopupService>();

            await popupService.ShowPopupAsync<PromptEditorPopupViewModel>(
                Shell.Current,
                options: new PopupOptions { Shape = null, Shadow = null }
            );
        }

        private void OnHtmlTabClicked(object? sender, EventArgs e) => ShowTab("html");

        private void OnRawTabClicked(object? sender, EventArgs e) => ShowTab("raw");

        private void ShowTab(string tab)
        {
            var primaryColor = (Color)Application.Current!.Resources["Primary"];
            var grayColor = (Color)Application.Current!.Resources["Gray400"];
            
            if (tab == "html")
            {
                HtmlContentGrid.IsVisible = true;
                RawContentGrid.IsVisible = false;
                HtmlTabButton.BackgroundColor = primaryColor;
                RawTabButton.BackgroundColor = grayColor;
            }
            else
            {
                HtmlContentGrid.IsVisible = false;
                RawContentGrid.IsVisible = true;
                HtmlTabButton.BackgroundColor = grayColor;
                RawTabButton.BackgroundColor = primaryColor;
            }
        }
    }
}

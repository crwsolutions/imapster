using CommunityToolkit.Maui.Views;

namespace Imapster.Popups
{
    public partial class EmailDetailsPopup : Popup
    {
        public EmailDetailsPopup(EmailDetailsViewModel viewModel)
        {
            BindingContext = viewModel;
            InitializeComponent();
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
    }
}

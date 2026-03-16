using CommunityToolkit.Maui.Views;

namespace Imapster.Popups
{
    public partial class EmailDetailsPopup : Popup
    {
        public EmailDetailsPopup(EmailViewModel email)
        {
            BindingContext = email;

            InitializeComponent();
        }

        private async void OnCloseClicked(object? sender, EventArgs e) => await CloseAsync();
    }
}

using CommunityToolkit.Maui.Views;

namespace Imapster.Popups
{
    public partial class EmailDetailsPopup : Popup
    {
        public EmailViewModel Email { get; }

        public EmailDetailsPopup(EmailViewModel email)
        {
            InitializeComponent();
            Email = email;
            BindingContext = Email;
        }

        private async void OnCloseClicked(object? sender, EventArgs e)
        {
            await CloseAsync();
        }
    }
}
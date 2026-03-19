using CommunityToolkit.Maui.Views;
using Imapster.HtmlRendering.Events;

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

            HtmlBodyView.LinkClicked += OnHtmlLinkClicked;
            HtmlBodyView.TextSelected += OnHtmlTextSelected;
        }

        private async void OnCloseClicked(object? sender, EventArgs e)
        {
            await CloseAsync();
        }

        private async void OnHtmlLinkClicked(object? sender, HtmlViewLinkClickedEventArgs e)
        {
            if (e is not null && !string.IsNullOrEmpty(e.Link))
            {
                try
                {
                    await Launcher.OpenAsync(e.Link);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to open link: {ex.Message}", "OK");
                }
            }
        }

        private void OnHtmlTextSelected(object? sender, HtmlViewTextSelectedEventArgs e)
        {
            // Handle text selection if needed
        }
    }
}
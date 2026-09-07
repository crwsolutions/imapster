using CommunityToolkit.Maui.Views;

namespace Imapster.Popups;

public partial class CancelAiPopup : Popup
{
    public CancelAiPopup(CancelAiViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

using CommunityToolkit.Maui.Views;

namespace Imapster.Popups;

public partial class PromptEditorPopup : Popup
{
    public PromptEditorPopup(PromptEditorPopupViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;

        Opened += async (s, e) => await ((PromptEditorPopupViewModel)BindingContext).LoadPromptCommand.ExecuteAsync(null);
    }
}
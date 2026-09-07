using CommunityToolkit.Maui.Views;

namespace Imapster.Popups;

public partial class EditAccountPopup : Popup<bool>
{
    public EditAccountPopup(AccountEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (BindingContext is not AccountEditorViewModel viewModel) return;

        await viewModel.SaveCommand.ExecuteAsync(null);
        await CloseAsync(viewModel.SaveSucceeded);
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await CloseAsync(false);
    }
}

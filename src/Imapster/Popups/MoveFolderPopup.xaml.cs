using CommunityToolkit.Maui.Views;

namespace Imapster.Popups;

public partial class MoveFolderPopup : Popup<string>
{
    public MoveFolderPopup(MoveFolderPopupViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;

        Opened += HandlePopupOpened;
    }

    private async void HandlePopupOpened(object? sender, EventArgs e)
    {
        if (BindingContext is MoveFolderPopupViewModel viewModel)
        {
            await viewModel.LoadFoldersCommand.ExecuteAsync(null);
        }
    }

    private async void MoveButtonClicked(object? sender, EventArgs e)
    {
        if (BindingContext is MoveFolderPopupViewModel viewModel && viewModel.SelectedFolder == null)
        {
            await Application.Current!.Windows[0]!.Page!.DisplayAlertAsync("Error", "Please select a folder.", "OK");
            return;
        }

        if (BindingContext is MoveFolderPopupViewModel vm && vm.SelectedFolder != null)
        {
            await CloseAsync(vm.SelectedFolder.Id);
        }
    }

    private void CancelButtonClicked(object? sender, EventArgs e)
    {
        CloseAsync();
    }
}
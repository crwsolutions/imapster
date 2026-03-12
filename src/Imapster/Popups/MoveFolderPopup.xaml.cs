using CommunityToolkit.Maui.Views;

namespace Imapster.Popups;

public partial class MoveFolderPopup : Popup<string>
{
    public MoveFolderPopup()
    {
        InitializeComponent();
        
        SaveButton.Clicked += SaveButtonClicked;
        CancelButton.Clicked += CancelButtonClicked;
        
        this.Loaded += HandlePopupOpened;
    }

    private async void HandlePopupOpened(object? sender, EventArgs e)
    {
        if (BindingContext is MoveFolderPopupViewModel viewModel)
        {
            await viewModel.LoadFoldersCommand.Execute(null);
        }
    }

    private async void SaveButtonClicked(object? sender, EventArgs e)
    {
        if (BindingContext is MoveFolderPopupViewModel viewModel && viewModel.SelectedFolder == null)
        {
            await Application.Current?.MainPage?.DisplayAlertAsync("Error", "Please select a folder.", "OK");
            return;
        }

        if (BindingContext is MoveFolderPopupViewModel vm && vm.SelectedFolder != null)
        {
            await CloseAsync(vm.SelectedFolder.Id);
        }
    }

    private void CancelButtonClicked(object? sender, EventArgs e)
    {
        CloseAsync(null);
    }

    protected override void OnClosed(bool value)
    {
        SaveButton.Clicked -= SaveButtonClicked;
        CancelButton.Clicked -= CancelButtonClicked;
        this.Loaded -= HandlePopupOpened;
    }
}
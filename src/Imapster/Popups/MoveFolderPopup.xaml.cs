using CommunityToolkit.Maui.Views;
using Imapster.Repositories;

namespace Imapster.Popups;

public partial class MoveFolderPopup : Popup<string>
{
    private MoveFolderPopupViewModel _viewModel;

    public MoveFolderPopup(IFolderRepository folderRepository, int accountId, string sourceFolderId)
    {
        InitializeComponent();
        _viewModel = new MoveFolderPopupViewModel();
        BindingContext = _viewModel;
        
        SaveButton.Clicked += SaveButtonClicked;
        CancelButton.Clicked += CancelButtonClicked;
        
        this.Loaded += HandlePopupOpened;
    }

    private async void HandlePopupOpened(object? sender, EventArgs e)
    {
        await _viewModel.LoadFoldersAsync(folderRepository, _accountId, _sourceFolderId);
    }

    private async void SaveButtonClicked(object? sender, EventArgs e)
    {
        if (_viewModel.SelectedFolder == null)
        {
            await Application.Current?.MainPage?.DisplayAlertAsync("Error", "Please select a folder.", "OK");
            return;
        }

        await CloseAsync(_viewModel.SelectedFolder.Id);
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
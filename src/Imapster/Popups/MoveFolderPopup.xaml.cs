using CommunityToolkit.Maui.Views;
using Imapster.Repositories;
using Imapster.ViewModels;

namespace Imapster.Popups;

public partial class MoveFolderPopup : Popup<string>
{
    private readonly IFolderRepository _folderRepository;
    private readonly int _accountId;
    private readonly string _sourceFolderId;
    private MoveFolderPopupViewModel _viewModel;

    public MoveFolderPopup(IFolderRepository folderRepository, int accountId, string sourceFolderId)
    {
        InitializeComponent();
        _folderRepository = folderRepository;
        _accountId = accountId;
        _sourceFolderId = sourceFolderId;
        _viewModel = new MoveFolderPopupViewModel(folderRepository, accountId, sourceFolderId);
        BindingContext = _viewModel;
        
        SaveButton.Clicked += SaveButtonClicked;
        CancelButton.Clicked += CancelButtonClicked;
    }

    private async void LoadButtonClicked(object? sender, EventArgs e)
    {
        await _viewModel.LoadFoldersAsync();
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
    }
}
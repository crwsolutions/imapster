using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Imapster.Repositories;

namespace Imapster.ViewModels;

public partial class MoveFolderPopupViewModel : ObservableObject
{
    private readonly IFolderRepository _folderRepository;
    private readonly int _accountId;
    private readonly string _sourceFolderId;

    [ObservableProperty]
    public partial ObservableCollection<FolderViewModel> AvailableFolders { get; set; } = [];

    [ObservableProperty]
    public partial FolderViewModel? SelectedFolder { get; set; }

    public MoveFolderPopupViewModel(int accountId, string sourceFolderId)
    {
        _accountId = accountId;
        _sourceFolderId = sourceFolderId;
        _folderRepository = new FolderRepository();
        LoadFoldersAsync();
    }

    private async Task LoadFoldersAsync()
    {
        var folders = await _folderRepository.GetAllFoldersAsync(_accountId);
        
        foreach (var folder in folders)
        {
            if (folder.Id != _sourceFolderId && !folder.IsTrash)
            {
                AvailableFolders.Add(folder);
            }
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (SelectedFolder == null)
        {
            await MainThread.InvokeOnMainThreadAsync(() => 
                Application.Current?.Windows[0].Page?.DisplayAlertAsync("Error", "Please select a folder.", "OK"));
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(() => 
            Application.Current?.Windows[0].Page?.ClosePopupAsync(true));
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await MainThread.InvokeOnMainThreadAsync(() => 
            Application.Current?.Windows[0].Page?.ClosePopupAsync(false));
    }
}
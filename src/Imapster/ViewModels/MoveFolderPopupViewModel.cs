using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Imapster.Repositories;

namespace Imapster.ViewModels;

public partial class MoveFolderPopupViewModel : ObservableObject, IQueryAttributable
{
    private IFolderRepository? _folderRepository;
    private int _accountId;
    private string _sourceFolderId = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<FolderViewModel> AvailableFolders { get; set; } = [];

    [ObservableProperty]
    public partial FolderViewModel? SelectedFolder { get; set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("folderRepository", out var repoObj) && repoObj is IFolderRepository folderRepository)
        {
            _folderRepository = folderRepository;
        }
        
        if (query.TryGetValue("accountId", out var accountIdObj) && accountIdObj is int accountId)
        {
            _accountId = accountId;
        }
        
        if (query.TryGetValue("sourceFolderId", out var sourceFolderIdObj) && sourceFolderIdObj is string sourceFolderId)
        {
            _sourceFolderId = sourceFolderId;
        }
    }

    [RelayCommand]
    private async Task LoadFoldersAsync()
    {
        if (_folderRepository == null) return;
        
        var folders = await _folderRepository.GetAllFoldersAsync(_accountId);
        
        foreach (var folder in folders)
        {
            if (folder.Id != _sourceFolderId && !folder.IsTrash)
            {
                AvailableFolders.Add(folder);
            }
        }
    }
}
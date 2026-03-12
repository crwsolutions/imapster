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

    public MoveFolderPopupViewModel(IFolderRepository folderRepository, int accountId, string sourceFolderId)
    {
        _folderRepository = folderRepository;
        _accountId = accountId;
        _sourceFolderId = sourceFolderId;
    }

    public async Task LoadFoldersAsync()
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
}
using CommunityToolkit.Mvvm.ComponentModel;
using Imapster.Repositories;

namespace Imapster.ViewModels;

public partial class MoveFolderPopupViewModel : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<FolderViewModel> AvailableFolders { get; set; } = [];

    [ObservableProperty]
    public partial FolderViewModel? SelectedFolder { get; set; }

    public async Task LoadFoldersAsync(IFolderRepository folderRepository, int accountId, string sourceFolderId)
    {
        var folders = await folderRepository.GetAllFoldersAsync(accountId);
        
        foreach (var folder in folders)
        {
            if (folder.Id != sourceFolderId && !folder.IsTrash)
            {
                AvailableFolders.Add(folder);
            }
        }
    }
}
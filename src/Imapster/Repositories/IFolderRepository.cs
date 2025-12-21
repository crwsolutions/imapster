using Imapster.ViewModels;

namespace Imapster.Repositories;

public interface IFolderRepository
{
    Task<List<FolderViewModel>> GetAllFoldersAsync(int accountId);
    Task<FolderViewModel> GetFolderByIdAsync(int accountId, string id);
    Task AddFolderAsync(FolderViewModel folder);
    Task UpdateFolderAsync(FolderViewModel folder);
    Task DeleteFolderAsync(int accountId, string id);
    Task UpsertFolderAsync(FolderViewModel folder);
}
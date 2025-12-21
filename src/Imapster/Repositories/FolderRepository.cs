using Dapper;
using Microsoft.Data.Sqlite;

namespace Imapster.Repositories;

public class FolderRepository : IFolderRepository
{
    private readonly string _dbPath;
    public FolderRepository()
    {
        _dbPath = Database.DbPath;
    }
    public async Task<List<FolderViewModel>> GetAllFoldersAsync(int accountId)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        var folders = await connection.QueryAsync<FolderViewModel>(
            "SELECT Id, Name, UnreadCount, IsTrash, AccountId FROM Folders WHERE AccountId = @AccountId",
            new { AccountId = accountId });
        return folders.ToList();
    }

    public async Task<FolderViewModel> GetFolderByIdAsync(int accountId, string id)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        var folder = await connection.QuerySingleOrDefaultAsync<FolderViewModel>(
            "SELECT Id, Name, UnreadCount, IsTrash, AccountId FROM Folders WHERE Id = @Id AND AccountId = @AccountId",
            new { Id = id, AccountId = accountId });
        return folder;
    }

    public async Task AddFolderAsync(FolderViewModel folder)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.ExecuteAsync(
            "INSERT INTO Folders (Id, Name, UnreadCount, IsTrash, AccountId) VALUES (@Id, @Name, @UnreadCount, @IsTrash, @AccountId)",
            folder);
    }

    public async Task UpdateFolderAsync(FolderViewModel folder)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.ExecuteAsync(
            "UPDATE Folders SET Name = @Name, UnreadCount = @UnreadCount, IsTrash = @IsTrash WHERE Id = @Id AND AccountId = @AccountId",
            folder);
    }
    public async Task UpsertFolderAsync(FolderViewModel folder)
    {
        var existingFolder = await GetFolderByIdAsync(folder.AccountId, folder.Id);
        if (existingFolder == null)
        {
            await AddFolderAsync(folder);
        }
        else
        {
            await UpdateFolderAsync(folder);
        }
    }

    public async Task DeleteFolderAsync(int accountId, string id)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.ExecuteAsync(
            "DELETE FROM Folders WHERE Id = @Id AND AccountId = @AccountId",
            new { Id = id, AccountId = accountId });
    }
}


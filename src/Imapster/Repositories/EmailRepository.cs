using Dapper;
using Microsoft.Data.Sqlite;

namespace Imapster.Repositories;

public class EmailRepository : IEmailRepository
{
    private readonly string _dbPath;

    public EmailRepository()
    {
        _dbPath = Database.DbPath;
    }

    public async Task<EmailViewModel> GetEmailByIdAsync(int accountId, string folderId, uint id)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        var email = await connection.QuerySingleOrDefaultAsync<EmailViewModel>(
            "SELECT Id, FromAddress as `From`, ToAddress as `To`, Date, Subject, Body, IsRead, FolderId, AccountId, HasAttachments, Size, AiSummary, AiCategory, AiDelete, AiDeleteMotivation FROM Emails WHERE Id = @Id AND AccountId = @AccountId AND FolderId = @FolderId",
            new { Id = id, AccountId = accountId, FolderId = folderId });
        return email;
    }

    public async Task<List<EmailViewModel>> GetEmailsByFolderIdAsync(int accountId, string folderId)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        var emails = await connection.QueryAsync<EmailViewModel>(
            "SELECT Id, FromAddress as `From`, ToAddress as `To`, Date, Subject, Body, IsRead, FolderId, AccountId, HasAttachments, Size, AiSummary, AiCategory, AiDelete, AiDeleteMotivation FROM Emails WHERE FolderId = @FolderId AND AccountId = @AccountId",
            new { FolderId = folderId, AccountId = accountId });
        return emails.ToList();
    }

    public async Task AddEmailAsync(EmailViewModel email)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.ExecuteAsync(
            "INSERT INTO Emails (Id, FromAddress, ToAddress, Date, Subject, Body, IsRead, FolderId, AccountId, HasAttachments, Size, AiSummary, AiCategory, AiDelete, AiDeleteMotivation) VALUES (@Id, @From, @To, @Date, @Subject, @Body, @IsRead, @FolderId, @AccountId, @HasAttachments, @Size, @AiSummary, @AiCategory, @AiDelete, @AiDeleteMotivation)",
            email);
    }

    public async Task UpdateEmailAsync(EmailViewModel email)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.ExecuteAsync(
            "UPDATE Emails SET FromAddress = @From, ToAddress = @To, Date = @Date, Subject = @Subject, Body = @Body, IsRead = @IsRead, HasAttachments = @HasAttachments, Size = @Size, AiSummary = @AiSummary, AiCategory = @AiCategory, AiDelete = @AiDelete, AiDeleteMotivation = @AiDeleteMotivation WHERE Id = @Id AND FolderId = @FolderId AND AccountId = @AccountId",
            email);
    }

    public async Task DeleteEmailAsync(int accountId, string folderId, uint id)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.ExecuteAsync(
            "DELETE FROM Emails WHERE Id = @Id AND AccountId = @AccountId AND FolderId = @FolderId",
            new { Id = id, AccountId = accountId, FolderId = folderId });
    }

    // New methods for IMAP sync
    public async Task<List<uint>> GetStoredEmailIdsByFolderIdAsync(int accountId, string folderId)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        var emailIds = await connection.QueryAsync<uint>(
            "SELECT Id FROM Emails WHERE FolderId = @FolderId AND AccountId = @AccountId",
            new { FolderId = folderId, AccountId = accountId });
        return emailIds.ToList();
    }

    public async Task<List<(uint uid, bool isRead)>> GetStoredEmailUidsAndFlagsByFolderIdAsync(int accountId, string folderId)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        var emailUids = await connection.QueryAsync<(uint uid, bool isRead)>(
            "SELECT Id as uid, IsRead as isRead FROM Emails WHERE FolderId = @FolderId AND AccountId = @AccountId",
            new { FolderId = folderId, AccountId = accountId });
        return emailUids.ToList();
    }

    public async Task UpdateEmailFlagsAsync(int accountId, string folderId, uint id, bool isRead)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.ExecuteAsync(
            "UPDATE Emails SET IsRead = @IsRead WHERE Id = @Id AND AccountId = @AccountId AND FolderId = @FolderId",
            new { Id = id, IsRead = isRead, AccountId = accountId, FolderId = folderId });
    }

    public async Task BulkInsertEmailsAsync(IEnumerable<EmailViewModel> emails)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");

        const string sql = @"
            INSERT OR REPLACE INTO Emails 
            (Id, FromAddress, ToAddress, Date, Subject, Body, IsRead, FolderId, AccountId, HasAttachments, Size, AiSummary, AiCategory, AiDelete, AiDeleteMotivation)
            VALUES (@Id, @From, @To, @Date, @Subject, @Body, @IsRead, @FolderId, @AccountId, @HasAttachments, @Size, @AiSummary, @AiCategory, @AiDelete, @AiDeleteMotivation)";

        await connection.ExecuteAsync(sql, emails);
    }

    public async Task BulkDeleteEmailsAsync(int accountId, string folderId, IEnumerable<uint> ids)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        const string sql = "DELETE FROM Emails WHERE Id = @Id AND AccountId = @AccountId AND FolderId = @FolderId";

        await connection.ExecuteAsync(
            sql,
            ids.Select(id => new { Id = id, AccountId = accountId, FolderId = folderId }),
            transaction
        );

        transaction.Commit();
    }

    public async Task BulkDeleteEmailsAsync(int accountId, string folderId)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.ExecuteAsync(
            "DELETE FROM Emails WHERE AccountId = @AccountId AND FolderId = @FolderId",
            new { AccountId = accountId, FolderId = folderId });
    }

    public async Task BulkMoveEmailsAsync(int accountId, string sourceFolderId, string targetFolderId, IEnumerable<uint> ids)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        const string sql = "UPDATE Emails SET FolderId = @TargetFolderId WHERE Id = @Id AND AccountId = @AccountId AND FolderId = @SourceFolderId";

        await connection.ExecuteAsync(
            sql,
            ids.Select(id => new { Id = id, AccountId = accountId, SourceFolderId = sourceFolderId, TargetFolderId = targetFolderId }),
            transaction
        );

        transaction.Commit();
    }
}
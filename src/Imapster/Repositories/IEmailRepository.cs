namespace Imapster.Repositories;

public interface IEmailRepository
{
    Task<EmailViewModel> GetEmailByIdAsync(int accountId, string folderId, uint id);
    Task<List<EmailViewModel>> GetEmailsByFolderIdAsync(int accountId, string folderId);
    Task AddEmailAsync(EmailViewModel email);
    Task UpdateEmailAsync(EmailViewModel email);
    Task DeleteEmailAsync(int accountId, string folderId, uint id);

    // New methods for IMAP sync
    Task<List<uint>> GetStoredEmailIdsByFolderIdAsync(int accountId, string folderId);
    Task<List<(uint uid, bool isRead)>> GetStoredEmailUidsAndFlagsByFolderIdAsync(int accountId, string folderId);
    Task UpdateEmailFlagsAsync(int accountId, string folderId, uint id, bool isRead);
    Task BulkInsertEmailsAsync(IEnumerable<EmailViewModel> emails);
    Task BulkDeleteEmailsAsync(int accountId, string folderId, IEnumerable<uint> ids);
    Task BulkDeleteEmailsAsync(int accountId, string folderId);
    Task BulkMoveEmailsAsync(int accountId, string sourceFolderId, string targetFolderId, IEnumerable<uint> ids);
}
using MimeKit;

namespace Imapster.Services;

public interface IImapSyncService : IDisposable
{
    Task<bool> ConnectAsync(ImapAccountViewModel account);
    Task EmailsAsync(string folderId, CancellationToken cancellationToken = default);

    Task FoldersAsync(CancellationToken cancellationToken = default);
    Task<string> MoveEmailsToTrashAsync(string sourceFolder, List<uint> emailIds);
    Task<string> MoveEmailsToFolderAsync(string sourceFolderId, string targetFolderId, List<uint> emailIds);
    Task EmptyFolderAsync(int accountId, string folderId);
    bool IsConnected();
    Task<MimeMessage> GetMessageAsync(int accountId, string folderId, uint emailId);
}

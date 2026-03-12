using Imapster.Repositories;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;

namespace Imapster.Services;

public class ImapSyncService : IImapSyncService
{
    private readonly ILogger<ImapSyncService> _logger;
    private readonly IEmailRepository _emailRepository;
    private readonly IFolderRepository _folderRepository;
    private ImapAccountViewModel? _currentAccount;
    private ImapClient? _imapClient;

    public ImapSyncService(ILogger<ImapSyncService> logger, IEmailRepository emailRepository, IFolderRepository folderRepository)
    {
        _logger = logger;
        _emailRepository = emailRepository;
        _folderRepository = folderRepository;
    }

    public async Task<bool> ConnectAsync(ImapAccountViewModel account)
    {
        try
        {
            _logger.LogInformation("Connecting to IMAP server: {Server}", account.Server);

            // Dispose previous connection if exists
            _imapClient?.Dispose();

            // Create new IMAP client
            _imapClient = new ImapClient();

            // Connect to the server
            await _imapClient.ConnectAsync(account.Server, account.Port, account.UseSsl);

            // Authenticate with credentials
            await _imapClient.AuthenticateAsync(account.Username, account.Password);

            _currentAccount = account;
            _logger.LogInformation("Successfully connected to IMAP server: {Server}", account.Server);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to IMAP server: {Server}", account.Server);
            return false;
        }
    }

    public async Task EmailsAsync(string folderId, CancellationToken cancellationToken = default)
    {
        if (_imapClient == null || _currentAccount == null)
        {
            _logger.LogWarning("IMAP client not connected. Cannot refresh emails.");
            return;
        }

        try
        {
            _logger.LogInformation("Refreshing emails for folder {FolderId}", folderId);

            // Get the folder
            var folder = await _imapClient.GetFolderAsync(folderId, cancellationToken);
            await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

            // 1. Get current UIDs from server (UniqueId list)
            var serverUniqueIds = (await folder.SearchAsync(SearchQuery.All, cancellationToken)).ToList();
            var serverUidsAsUInts = serverUniqueIds.Select(u => u.Id).ToHashSet();

            // 2. Get known UIDs from local store
            var localUids = (await _emailRepository.GetStoredEmailIdsByFolderIdAsync(_currentAccount.Id, folderId)).ToHashSet();

            // 3. Detect new messages (work with UniqueId list for fetch)
            var newUniqueIds = serverUniqueIds.Where(uid => !localUids.Contains(uid.Id)).ToList();
            if (newUniqueIds.Count > 0)
            {
                _logger.LogInformation("Found {Count} new emails to sync", newUniqueIds.Count);
                var newMessages = await folder.FetchAsync(newUniqueIds, 
                    MessageSummaryItems.UniqueId | 
                    MessageSummaryItems.Envelope | 
                    MessageSummaryItems.Flags |
                    MessageSummaryItems.Size |
                    MessageSummaryItems.BodyStructure,
                    cancellationToken);
                foreach (var summary in newMessages)
                {
                    var message = await folder.GetMessageAsync(summary.UniqueId, cancellationToken);
                    await _emailRepository.AddEmailAsync(EmailViewModel.FromMessage(message, summary.UniqueId, folderId, _currentAccount.Id, summary.Size, summary.Flags));
                }
            }

            // 4. Detect deleted messages
            var deletedUids = localUids.Except(serverUidsAsUInts).ToList();
            if (deletedUids.Count > 0)
            {
                _logger.LogInformation("Found {Count} deleted emails to remove", deletedUids.Count);
                await _emailRepository.BulkDeleteEmailsAsync(_currentAccount.Id, folderId, deletedUids);
            }

            // 5. Detect flag changes
            var commonUniqueIds = serverUniqueIds.Where(uid => localUids.Contains(uid.Id)).ToList();
            if (commonUniqueIds.Any())
            {
                var summaries = await folder.FetchAsync(commonUniqueIds, MessageSummaryItems.UniqueId | MessageSummaryItems.Flags, cancellationToken);

                // fetch stored flags once to avoid repeated I/O inside the loop
                var storedFlags = await _emailRepository.GetStoredEmailUidsAndFlagsByFolderIdAsync(_currentAccount.Id, folderId);

                foreach (var summary in summaries)
                {
                    var currentFlags = summary.Flags ?? MessageFlags.None;
                    var storedFlag = storedFlags.FirstOrDefault(f => f.uid == summary.UniqueId.Id);

                    var isReadNow = IsReadFromFlags(currentFlags);
                    if (storedFlag.isRead != isReadNow)
                    {
                        await _emailRepository.UpdateEmailFlagsAsync(_currentAccount.Id, folderId, summary.UniqueId.Id, isReadNow);
                    }
                }
            }

            _logger.LogInformation("Successfully refreshed emails for folder {FolderId}", folderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh emails for folder {FolderId}", folderId);
            throw;
        }
    }

    public async Task FoldersAsync(CancellationToken cancellationToken = default)
    {
        if (_imapClient == null || _currentAccount == null)
        {
            _logger.LogWarning("IMAP client not connected. Cannot refresh folders.");
            return;
        }

        try
        {
            _logger.LogInformation("Refreshing folders");

            // Get the folder list using the correct MailKit API
            var folders = await _imapClient.GetFoldersAsync(_imapClient.PersonalNamespaces[0], cancellationToken: cancellationToken);

            // For each folder, create or update it in the repository
            foreach (var folder in folders)
            {
                // Use existing method to store folder information
                await _folderRepository.UpsertFolderAsync(new FolderViewModel
                {
                    Id = folder.FullName,
                    Name = folder.Name,
                    AccountId = _currentAccount.Id,
                    IsTrash = folder.Attributes.HasFlag(FolderAttributes.Trash)
                });
            }

            _logger.LogInformation("Successfully refreshed folders");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh folders");
            throw;
        }
    }

    private bool IsReadFromFlags(MailKit.MessageFlags flags)
    {
        return flags.HasFlag(MailKit.MessageFlags.Seen);
    }

    public async Task<string> MoveEmailsToTrashAsync(string folderId, List<uint> emailIds)
    {
        if (_imapClient == null || _currentAccount == null)
        {
            throw new ApplicationException("IMAP client not connected. Cannot move emails to trash.");
        }

        var trashFolder = GetTrashFolder() ?? throw new ApplicationException($"No trash folder found for account {_currentAccount.Id}");
        var source = GetFolder(folderId) ?? throw new ApplicationException($"Source folder {folderId} not found");

        await source.OpenAsync(FolderAccess.ReadWrite);

        // Move emails to trash folder on the server
        foreach (var emailId in emailIds)
        {
            // Mark the message as deleted on the server
            var uid = new UniqueId(emailId);
            await source.MoveToAsync(uid, trashFolder);
        }
        await source.CloseAsync();

        await _emailRepository.BulkDeleteEmailsAsync(_currentAccount.Id, folderId, emailIds);

        return $"Moved {emailIds.Count} emails to trash";
    }

    public async Task<string> MoveEmailsToFolderAsync(string sourceFolderId, string targetFolderId, List<uint> emailIds)
    {
        if (_imapClient == null || _currentAccount == null)
        {
            throw new ApplicationException("IMAP client not connected. Cannot move emails.");
        }

        if (sourceFolderId == targetFolderId)
        {
            throw new ApplicationException("Source and target folders must be different.");
        }

        var source = GetFolder(sourceFolderId) ?? throw new ApplicationException($"Source folder {sourceFolderId} not found");
        var target = GetFolder(targetFolderId) ?? throw new ApplicationException($"Target folder {targetFolderId} not found");

        await source.OpenAsync(FolderAccess.ReadWrite);

        // Move emails on the server
        var uids = emailIds.Select(id => new UniqueId(id)).ToList();
        await source.MoveToAsync(uids, target);

        await source.CloseAsync();

        // Update local database
        await _emailRepository.BulkMoveEmailsAsync(_currentAccount.Id, sourceFolderId, targetFolderId, emailIds);

        return $"Moved {emailIds.Count} emails to {target.Name}";
    }

    IMailFolder? GetTrashFolder()
    {
        var personal = _imapClient!.GetFolder(_imapClient.PersonalNamespaces[0]);

        var candidates = personal.GetSubfolders(false)
            .Where(f =>
                f.Attributes.HasFlag(FolderAttributes.Trash) ||
                f.Name.Equals("Trash", StringComparison.OrdinalIgnoreCase) ||
                f.Name.Equals("Deleted Items", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return candidates.FirstOrDefault();
    }

    IMailFolder? GetFolder(string id)
    {
        var personal = _imapClient?.GetFolder(_imapClient.PersonalNamespaces[0]);
        var folders = personal?.GetSubfolders(false);
        var folder = folders?.FirstOrDefault(f => f.FullName == id);
        return folder;
    }

    public void Dispose()
    {
        _imapClient?.Dispose();
    }

    public async Task EmptyFolderAsync(int accountId, string folderId)
    {
        if (_imapClient == null || _currentAccount == null)
        {
            throw new ApplicationException("IMAP client not connected. Cannot empty folder.");
        }

        if (accountId != _currentAccount.Id)
        {
            throw new ApplicationException($"Account mismatch. Expected {_currentAccount.Id}, got {accountId}.");
        }

        try
        {
            _logger.LogInformation("Emptying folder {FolderId}", folderId);

            // Get the folder
            var folder = await _imapClient.GetFolderAsync(folderId);
            if (folder == null)
            {
                throw new ApplicationException($"Folder {folderId} not found");
            }

            await folder.OpenAsync(FolderAccess.ReadWrite);

            // Get all email UIDs in the folder
            var allUids = (await folder.SearchAsync(SearchQuery.All)).ToList();

            if (allUids.Count > 0)
            {
                // Mark all messages as deleted
                await folder.AddFlagsAsync(allUids, MailKit.MessageFlags.Deleted, true);

                // Expunge (permanently delete) all marked messages
                await folder.ExpungeAsync();

                _logger.LogInformation("Deleted {Count} emails from folder {FolderId}", allUids.Count, folderId);
            }

            await folder.CloseAsync();

            // Also delete from local database in one operation
            await _emailRepository.BulkDeleteEmailsAsync(accountId, folderId);

            _logger.LogInformation("Successfully emptied folder {FolderId}", folderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to empty folder {FolderId}", folderId);
            throw;
        }
    }
}


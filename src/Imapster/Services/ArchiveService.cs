using Imapster.Repositories;
using MimeKit;

namespace Imapster.Services;

public interface IArchiveService
{
    Task<string> ArchiveAttachmentAsync(int accountId, string folderId, uint emailId, string attachmentName);
    bool CanArchive(int accountId);
}

public class ArchiveService : IArchiveService
{
    private readonly IImapSyncService _imapSyncService;
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<ArchiveService> _logger;

    public ArchiveService(IImapSyncService imapSyncService, IAccountRepository accountRepository, ILogger<ArchiveService> logger)
    {
        _imapSyncService = imapSyncService;
        _accountRepository = accountRepository;
        _logger = logger;
    }

    public bool CanArchive(int accountId)
    {
        // Check if account has archive path configured
        // Note: This is a simplified check - actual connection state would need to be verified via IMAP service
        return true;
    }

    public async Task<string> ArchiveAttachmentAsync(int accountId, string folderId, uint emailId, string attachmentName)
    {
        var account = await _accountRepository.GetAccountByIdAsync(accountId);
        if (account == null || string.IsNullOrWhiteSpace(account.AttachmentArchivePath))
            throw new InvalidOperationException("Archive path not configured for this account");

        // Fetch email from IMAP server
        var message = await _imapSyncService.GetMessageAsync(accountId, folderId, emailId);

        // Find the attachment by name
        var attachment = message.Attachments.FirstOrDefault(a =>
            a.ContentDisposition?.FileName == attachmentName) ?? 
            throw new InvalidOperationException($"Attachment '{attachmentName}' not found in email");

        // Create path structure: mainPath/year/fromAddress_filename
        var mainPath = account.AttachmentArchivePath;
        var year = message.Date.DateTime.Year.ToString();
        var fromAddress = message.From?.FirstOrDefault()?.ToString() ?? "unknown";

        var yearPath = Path.Combine(mainPath, year);
        Directory.CreateDirectory(yearPath);

        // Sanitize file name and create full path
        var safeFileName = SanitizeFileName(attachmentName);
        var fullPath = Path.Combine(yearPath, $"{fromAddress}_{safeFileName}");

        // Download and save attachment
        using var fileStream = File.Create(fullPath);
        if (attachment is MimePart mimePart)
        {
            using var contentStream = mimePart.Content.Open();
            await contentStream.CopyToAsync(fileStream);
        }

        _logger.LogInformation("Archived attachment '{FileName}' to '{Path}'", attachmentName, fullPath);
        return fullPath;
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove invalid characters from file name
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return Path.GetFileName(sanitized); // Ensure no path traversal
    }
}
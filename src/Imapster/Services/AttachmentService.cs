using Imapster.Repositories;
using MimeKit;

namespace Imapster.Services;

public interface IAttachmentService
{
    Task<string> ArchiveAttachmentAsync(int accountId, string folderId, uint emailId, string attachmentName);
    Task<string> OpenAttachmentAsync(int accountId, string folderId, uint emailId, string attachmentName);
}

public class AttachmentService : IAttachmentService
{
    private readonly IImapSyncService _imapSyncService;
    private readonly IAccountRepository _accountRepository;

    public AttachmentService(IImapSyncService imapSyncService, IAccountRepository accountRepository)
    {
        _imapSyncService = imapSyncService;
        _accountRepository = accountRepository;
    }

    public async Task<string> ArchiveAttachmentAsync(int accountId, string folderId, uint emailId, string attachmentName)
    {
        var account = await _accountRepository.GetAccountByIdAsync(accountId);
        if (account == null || string.IsNullOrWhiteSpace(account.AttachmentArchivePath))
            throw new InvalidOperationException("Archive path not configured for this account");

        // Fetch email and attachment using shared method
        var message = await _imapSyncService.GetMessageAsync(accountId, folderId, emailId);
        var attachment = await GetAttachmentFromEmailAsync(accountId, folderId, emailId, attachmentName);

        // Create path structure: mainPath/year/fromAddress_filename
        var mainPath = account.AttachmentArchivePath;
        var year = message.Date.DateTime.Year.ToString();
        var from = message.From?.FirstOrDefault();
        var fromAddress = from switch
        {
            MailboxAddress mb => mb.Address,
            GroupAddress g => g.Name,
            _ => "unknown"
        };

        var yearPath = Path.Combine(mainPath, year);
        Directory.CreateDirectory(yearPath);

        // Sanitize file name and create full path
        var safeFileName = SanitizeFileName(attachmentName);
        var fullPath = Path.Combine(yearPath, $"{fromAddress}_{safeFileName}");

        // Use shared download method
        await DownloadAttachmentToPathAsync(message, attachmentName, fullPath);

        return fullPath;
    }

    /// <summary>
    /// Opens an attachment by downloading it to the temp directory and returning the path.
    /// </summary>
    public async Task<string> OpenAttachmentAsync(int accountId, string folderId, uint emailId, string attachmentName)
    {
        // Fetch email and attachment using shared methods
        var message = await _imapSyncService.GetMessageAsync(accountId, folderId, emailId);
        var attachment = await GetAttachmentFromEmailAsync(accountId, folderId, emailId, attachmentName);

        // Create temp directory path: FileSystem.AppDataDirectory + "temp"
        var tempDir = Path.Combine(FileSystem.AppDataDirectory, "temp");
        Directory.CreateDirectory(tempDir);

        // Sanitize file name and create full path
        var safeFileName = SanitizeFileName(attachmentName);
        var fullPath = Path.Combine(tempDir, safeFileName);

        // Use shared download method
        await DownloadAttachmentToPathAsync(message, attachmentName, fullPath);

        return fullPath;
    }

    /// <summary>
    /// Downloads an attachment from an email to a specified path.
    /// Shared method used by ArchiveAttachmentAsync and OpenAttachmentAsync.
    /// </summary>
    internal async Task DownloadAttachmentToPathAsync(MimeMessage message, string attachmentName, string fullPath)
    {
        // Find the attachment by name (check both ContentDisposition.FileName and ContentType.Name)
        var attachment = message.Attachments.FirstOrDefault(a =>
            a.ContentDisposition?.FileName == attachmentName || a.ContentType?.Name == attachmentName) ??
            throw new InvalidOperationException($"Attachment '{attachmentName}' not found in email");

        // Download and save attachment
        using var fileStream = File.Create(fullPath);
        if (attachment is MimePart mimePart && mimePart.Content is not null)
        {
            using var contentStream = mimePart.Content.Open();
            await contentStream.CopyToAsync(fileStream);
        }
    }

    /// <summary>
    /// Fetches an email and finds an attachment by name.
    /// Shared method used by ArchiveAttachmentAsync and OpenAttachmentAsync.
    /// </summary>
    internal async Task<MimeEntity> GetAttachmentFromEmailAsync(int accountId, string folderId, uint emailId, string attachmentName)
    {
        var message = await _imapSyncService.GetMessageAsync(accountId, folderId, emailId);

        var attachment = message.Attachments.FirstOrDefault(a =>
            a.ContentDisposition?.FileName == attachmentName || a.ContentType?.Name == attachmentName) ??
            throw new InvalidOperationException($"Attachment '{attachmentName}' not found in email");

        return attachment;
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove invalid characters from file name
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return Path.GetFileName(sanitized); // Ensure no path traversal
    }
}

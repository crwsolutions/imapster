using Imapster.Repositories;
using Imapster.Services;

namespace Imapster.ViewModels;

public partial class EmailDetailsViewModel : ObservableObject, IQueryAttributable
{
    private readonly IArchiveService _archiveService;
    private readonly IImapSyncService _imapSyncService;
    private readonly IAccountRepository _accountRepository;

    public EmailDetailsViewModel(IArchiveService archiveService, IImapSyncService imapSyncService, IAccountRepository accountRepository)
    {
        _archiveService = archiveService;
        _imapSyncService = imapSyncService;
        _accountRepository = accountRepository;
    }

    [ObservableProperty]
    private bool _canArchive = false;

    [ObservableProperty]
    public partial EmailViewModel? Email { get; set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("email", out var emailObj) && emailObj is EmailViewModel email)
        {
            Email = email;
            UpdateCanArchive();
        }
    }

    private void UpdateCanArchive()
    {
        // Can archive if account has archive path configured and is connected
        var account = _accountRepository.GetAccountByIdAsync(Email!.AccountId).Result;
        var hasPath = !string.IsNullOrWhiteSpace(account?.AttachmentArchivePath);
        var isConnected = _imapSyncService.IsConnected();
        CanArchive = hasPath && isConnected;
    }

    [RelayCommand]
    private async Task ArchiveAttachmentAsync(AttachmentViewModel attachment)
    {
        try
        {
            var result = await _archiveService.ArchiveAttachmentAsync(
                Email!.AccountId,
                Email.FolderId,
                Email.Id,
                attachment.FileName);

            await Application.Current!.Windows[0]!.Page!.DisplayAlertAsync(
                "Success",
                $"Attachment archived to: {result}",
                "OK");
        }
        catch (Exception ex)
        {
            await Application.Current!.Windows[0]!.Page!.DisplayAlertAsync(
                "Error",
                $"Failed to archive attachment: {ex.Message}",
                "OK");
        }
    }
}
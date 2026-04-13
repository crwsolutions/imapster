using CommunityToolkit.Maui.Extensions;
using Imapster.Popups;
using Imapster.Repositories;
using Imapster.Services;

namespace Imapster.ViewModels;

public partial class EmailDetailsViewModel : ObservableObject, IQueryAttributable
{
    private readonly IArchiveService _archiveService;
    private readonly IImapSyncService _imapSyncService;
    private readonly IAccountRepository _accountRepository;
    private EmailAiService? _emailAiService;
    private IEmailRepository? _emailRepository;

    private CancellationTokenSource? _cancellationTokenSource;

    public EmailDetailsViewModel(IArchiveService archiveService, IImapSyncService imapSyncService, IAccountRepository accountRepository, EmailAiService? emailAiService, IEmailRepository? emailRepository)
    {
        _archiveService = archiveService;
        _imapSyncService = imapSyncService;
        _accountRepository = accountRepository;
        _emailAiService = emailAiService;
        _emailRepository = emailRepository;
    }

    [ObservableProperty]
    private bool _canArchive = false;

    [ObservableProperty]
    public partial EmailViewModel Email { get; set; } = default!;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? Status { get; set; }

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

    [RelayCommand]
    private async Task RedoAiClassificationAsync()
    {
        // Cancel any ongoing classification first
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();

        Status = "Running AI classification...";
        IsBusy = true;

        _emailAiService ??= App.Services.GetRequiredService<EmailAiService>();
        _emailRepository ??= App.Services.GetRequiredService<IEmailRepository>();

        _cancellationTokenSource = new CancellationTokenSource();

        // Show cancel popup
        var popup = new CancelAiPopup(_cancellationTokenSource);
        Application.Current!.Windows[0].Page!.ShowPopup(popup, new PopupOptions
        {
            CanBeDismissedByTappingOutsideOfPopup = false
        });

        try
        {
            var classification = await _emailAiService.ClassifyEmailAsync(Email, _cancellationTokenSource.Token);
            Email.AiSummary = classification.Summary;
            Email.AiCategory = classification.Category;
            Email.AiDelete = classification.Delete;
            Email.AiDeleteMotivation = classification.Reason;
            await _emailRepository.UpdateEmailAsync(Email);
            Status = "AI classification completed";
        }
        catch (OperationCanceledException)
        {
            Status = "AI classification cancelled";
        }
        catch (Exception exception)
        {
            // Set error state and save the email
            Email.AiCategory = "Error";
            Email.AiSummary = exception.Message;
            await _emailRepository.UpdateEmailAsync(Email);
            Status = $"AI classification failed: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
            await popup.CloseAsync(false);
        }
    }

    [RelayCommand]
    private void CancelAiClassification()
    {
        _cancellationTokenSource?.Cancel();
        Status = "AI classification cancelled";
    }
}
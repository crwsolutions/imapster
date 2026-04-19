using CommunityToolkit.Maui.Extensions;
using Imapster.ContentViews;
using Imapster.Extensions;
using Imapster.Popups;
using Imapster.Repositories;
using Imapster.Services;
using MailKit;
using MimeKit;
using System.Text.RegularExpressions;

namespace Imapster.ViewModels;

[DebuggerDisplay("Email: From = {From}, Date = {Date} '{Subject}'")]
public partial class EmailViewModel : ObservableObject, IDataGridItem, IEquatable<EmailViewModel>
{
    private IAttachmentService? _attachmentService;
    private IImapSyncService? _imapSyncService;
    private IAccountRepository? _accountRepository;
    private EmailAiService? _emailAiService;
    private IEmailRepository? _emailRepository;

    private CancellationTokenSource? _cancellationTokenSource;

    [GeneratedRegex("<[^>]+>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlRegEx { get; }

    [ObservableProperty]
    public partial uint Id { get; set; } = default!;

    [ObservableProperty]
    public partial string From { get; set; } = default!;

    [ObservableProperty]
    public partial string To { get; set; } = default!;

    [ObservableProperty]
    public partial DateTime Date { get; set; } = default!;

    [ObservableProperty]
    public partial string Subject { get; set; } = default!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BodyHtml))]
    public partial string Body { get; set; } = default!;

    public string BodyHtml => HtmlRegEx.IsMatch(Body) ?  Body.Trim() : $"<html><pre>{Body.Trim()}</pre></html>";

    [ObservableProperty]
    public partial bool IsRead { get; set; } = default!;

    [ObservableProperty]
    public partial string FolderId { get; set; } = default!;

    [ObservableProperty]
    public partial int AccountId { get; set; }

    [ObservableProperty]
    public partial uint? Size { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAttachments))]
    [NotifyPropertyChangedFor(nameof(AttachmentList))]
    public partial string? Attachments { get; set; }

    public bool HasAttachments => !string.IsNullOrWhiteSpace(Attachments);

    public ObservableCollection<AttachmentViewModel> AttachmentList =>
        string.IsNullOrWhiteSpace(Attachments)
            ? []
            : new ObservableCollection<AttachmentViewModel>(
                Attachments
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(a => new AttachmentViewModel { FileName = a.Trim() })
            );

    // AI Properties
    [ObservableProperty]
    public partial string? AiSummary { get; set; }

    [ObservableProperty]
    public partial string? AiCategory { get; set; }

    [ObservableProperty]
    public partial bool? AiDelete { get; set; }

    [ObservableProperty]
    public partial string? AiDeleteMotivation { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public string FormattedSize => Size is null ? "-" : Size.Value.FormatSize();

    [ObservableProperty]
    public partial bool CanArchive { get; set; }

    public static EmailViewModel FromMessage(MimeMessage message, UniqueId id, string folderId, int accountId, uint? size, MessageFlags? flags)
    {
        var attachments = message.Attachments
            .OfType<MimeEntity>()
            .Select(a => 
            {
                // Try ContentDisposition.FileName first, fall back to ContentType.Name
                var fileName = a.ContentDisposition?.FileName ?? a.ContentType?.Name;
                return fileName;
            })
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .ToList();
        var attachmentsString = attachments.Count > 0
            ? string.Join(",", attachments)
            : null;

        return new EmailViewModel
        {
            Id = id.Id,
            From = message.From.ToString(),
            To = message.To.ToString(),
            Date = message.Date.DateTime,
            Subject = message.Subject ?? "-",
            Body = message.HtmlBody?.Trim() ?? message.TextBody?.Trim() ?? "",
            IsRead = flags?.HasFlag(MessageFlags.Seen) is true,
            FolderId = folderId,
            AccountId = accountId,
            Size = size,
            Attachments = attachmentsString
        };
    }

    public object? GetValue(string key) => key switch
    {
        nameof(From) => From,
        nameof(To) => To,
        nameof(AiDelete) => AiDelete,
        nameof(Id) => Id,
        nameof(Date) => Date,
        nameof(Subject) => Subject,
        nameof(Body) => Body,
        nameof(IsRead) => IsRead,
        nameof(FolderId) => FolderId,
        nameof(AccountId) => AccountId,
        nameof(AiSummary) => AiSummary,
        nameof(Size) => Size,
        nameof(Attachments) => Attachments,
        nameof(AiCategory) => AiCategory,
        nameof(AiDeleteMotivation) => AiDeleteMotivation,
        _ => null,
    };

    override public int GetHashCode() => HashCode.Combine(Id, FolderId, AccountId);

    override public bool Equals(object? obj)
    {
        if (obj is EmailViewModel other)
        {
            return Equals(other);
        }
        return base.Equals(obj);
    }

    public bool Equals(EmailViewModel? other) =>
        other is not null &&
        Id == other.Id &&
        FolderId == other.FolderId &&
        AccountId == other.AccountId;

    internal void UpdateCanArchive()
    {
        _accountRepository ??= App.Services.GetRequiredService<IAccountRepository>();
        _imapSyncService ??= App.Services.GetRequiredService<IImapSyncService>();

        // Can archive if account has archive path configured and is connected
        var account = _accountRepository.GetAccountByIdAsync(AccountId).Result;
        var hasPath = !string.IsNullOrWhiteSpace(account?.AttachmentArchivePath);
        var isConnected = _imapSyncService.IsConnected();
        CanArchive = hasPath && isConnected;
    }

    [RelayCommand]
    private async Task ArchiveAttachmentAsync(AttachmentViewModel attachment)
    {
        try
        {
            _attachmentService ??= App.Services.GetRequiredService<IAttachmentService>();
            var result = await _attachmentService.ArchiveAttachmentAsync(
                AccountId,
                FolderId,
                Id,
                attachment.FileName);

            var openDocument = await Application.Current!.Windows[0]!.Page!.DisplayAlertAsync(
                "Success",
                $"Attachment archived to: {result}. Do you want to open the document?",
                "Yes",
                "No");

            if (openDocument)
            {
                Process.Start(new ProcessStartInfo(result) { UseShellExecute = true });
            }
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

        var main = App.Services.GetRequiredService<MainViewModel>();

        main.StatusText = "Running AI classification...";
        main.IsBusy = true;

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
            var classification = await _emailAiService.ClassifyEmailAsync(this, _cancellationTokenSource.Token);
            AiSummary = classification.Summary;
            AiCategory = classification.Category;
            AiDelete = classification.Delete;
            AiDeleteMotivation = classification.Reason;
            await _emailRepository.UpdateEmailAsync(this);
            main.StatusText = "AI classification completed";
        }
        catch (OperationCanceledException)
        {
            main.StatusText = "AI classification cancelled";
        }
        catch (Exception exception)
        {
            // Set error state and save the email
            AiCategory = "Error";
            AiSummary = exception.Message;
            await _emailRepository.UpdateEmailAsync(this);
            main.StatusText = $"AI classification failed: {exception.Message}";
        }
        finally
        {
            main.IsBusy = false;
            await popup.CloseAsync(false);
        }
    }

    [RelayCommand]
    private void CancelAiClassification()
    {
        _cancellationTokenSource?.Cancel();
        var main = App.Services.GetRequiredService<MainViewModel>();
        main.StatusText = "AI classification cancelled";
    }

    [RelayCommand]
    private async Task OpenAttachmentAsync(AttachmentViewModel attachment)
    {
        try
        {
            _attachmentService ??= App.Services.GetRequiredService<IAttachmentService>();
            var tempPath = await _attachmentService.OpenAttachmentAsync(
                AccountId,
                FolderId,
                Id,
                attachment.FileName);

            Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            await Application.Current!.Windows[0]!.Page!.DisplayAlertAsync(
                "Error",
                $"Failed to open attachment: {ex.Message}",
                "OK");
        }
    }

    [RelayCommand]
    private async Task ReFetchEmailAsync()
    {
        var main = App.Services.GetRequiredService<MainViewModel>();

        try
        {
            main.StatusText = "Re-fetching email...";
            main.IsBusy = true;

            _imapSyncService ??= App.Services.GetRequiredService<IImapSyncService>();
            _emailRepository ??= App.Services.GetRequiredService<IEmailRepository>();

            if (!_imapSyncService.IsConnected())
            {
                main.StatusText = "Not connected to IMAP server";
                return;
            }

            var message = await _imapSyncService.GetMessageAsync(AccountId, FolderId, Id);

            var newEmail = FromMessage(
                message,
                new UniqueId(Id),
                FolderId,
                AccountId,
                Size,
                null);

            From = newEmail.From;
            To = newEmail.To;
            Date = newEmail.Date;
            Subject = newEmail.Subject;
            Body = newEmail.Body;
            IsRead = newEmail.IsRead;
            Size = newEmail.Size;
            Attachments = newEmail.Attachments;
            AiSummary = newEmail.AiSummary;
            AiCategory = newEmail.AiCategory;
            AiDelete = newEmail.AiDelete;
            AiDeleteMotivation = newEmail.AiDeleteMotivation;
            main.StatusText = "Email re-fetched successfully";

            await _emailRepository.UpdateEmailAsync(this);
        }
        catch (Exception ex)
        {
            main.StatusText = $"Failed to re-fetch email: {ex.Message}";
        }
        finally
        {
            main.IsBusy = false;
        }
    }
}

using CommunityToolkit.Maui.Extensions;
using Imapster.ContentViews;
using Imapster.Popups;
using Imapster.Repositories;
using Imapster.Services;
using MailKit;
using MimeKit;

namespace Imapster.ViewModels;

[DebuggerDisplay("Email: From = {From}, Date = {Date} '{Subject}'")]
public partial class EmailViewModel : ObservableObject, IDataGridItem, IEquatable<EmailViewModel>
{
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
    public partial string Body { get; set; } = default!;

    [ObservableProperty]
    public partial bool IsRead { get; set; } = default!;

    [ObservableProperty]
    public partial string FolderId { get; set; } = default!;

    [ObservableProperty]
    public partial int AccountId { get; set; }

    [ObservableProperty]
    public partial string? Attachments { get; set; }

    [ObservableProperty]
    public partial uint? Size { get; set; }

    public bool HasAttachments => !string.IsNullOrWhiteSpace(Attachments);

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

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? Status { get; set; }

    public EmailViewModel()
    {

    }

    public string FormattedSize => Size is null ? "-" : FormatSize(Size.Value);

    private static string FormatSize(long bytes)
    {
        const double KB = 1024;
        const double MB = KB * 1024;
        const double GB = MB * 1024;

        if (bytes < KB)
            return $"{bytes} B";

        if (bytes < MB)
            return $"{bytes / KB:0} KB";

        if (bytes < GB)
            return $"{bytes / MB:0.0} MB";

        return $"{bytes / GB:0.0} GB";
    }

    public MimeMessage ToMimeMessage()
    {
        var message = new MimeMessage();
        message.MessageId = Id.ToString();
        message.From.Add(MailboxAddress.Parse(From));
        try
        {
            message.To.Add(MailboxAddress.Parse(To));
        }
        catch
        {
            //pitty
        }
        message.Date = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(Date).ToUnixTimeSeconds());
        message.Subject = Subject;
        message.Body = new TextPart("plain") { Text = Body };
        return message;
    }

    public EmailViewModel(uint id, string from, string to, DateTime date, string subject, string body, bool isRead, string folderId, int accountId, uint? size, string? attachments)
    {
        Id = id;
        From = from;
        To = to;
        Date = date;
        Subject = subject;
        Body = body;
        IsRead = isRead;
        FolderId = folderId;
        AccountId = accountId;
        Size = size;
        Attachments = attachments;
    }

    public static EmailViewModel FromMessage(MimeMessage message, UniqueId id, string folderId, int accountId, uint? size, MessageFlags? flags)
    {
        var attachments = message.Attachments
            .OfType<MimeEntity>()
            .Where(a => !string.IsNullOrWhiteSpace(a.ContentDisposition?.FileName))
            .Select(a => a.ContentDisposition!.FileName!)
            .ToList();
        var attachmentsString = attachments.Count > 0 
            ? string.Join(",", attachments) 
            : null;

        return new EmailViewModel(
            id.Id,
            message.From.ToString(),
            message.To.ToString(),
            message.Date.DateTime,
            message.Subject,
            message.TextBody ?? message.HtmlBody ?? "",
            flags?.HasFlag(MessageFlags.Seen) is true,
            folderId,
            accountId,
            size,
            attachmentsString
        );
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

    private EmailAiService? _emailAiService;
    private IEmailRepository? _emailRepository;

    [RelayCommand]
    private async Task ShowDetailsAsync() =>
        await Shell.Current.ShowPopupAsync(new EmailDetailsPopup(this));

    [RelayCommand]
    private async Task RedoAiClassificationAsync()
    {
        Status = "Running AI classification...";

        IsBusy = true;

        _emailAiService ??= App.Services.GetRequiredService<EmailAiService>();
        _emailRepository ??= App.Services.GetRequiredService<IEmailRepository>();

        try
        {
            var classification = await _emailAiService.ClassifyEmailAsync(ToMimeMessage());
            AiSummary = classification.Summary;
            AiCategory = classification.Category;
            AiDelete = classification.Delete;
            AiDeleteMotivation = classification.Reason;
            await _emailRepository.UpdateEmailAsync(this);
            Status = "AI classification completed";
        }
        catch(Exception exception)
        {
            Status = $"AI classification failed: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public bool Equals(EmailViewModel? other) => 
        other is not null &&
        Id == other.Id &&
        FolderId == other.FolderId &&
        AccountId == other.AccountId;
}
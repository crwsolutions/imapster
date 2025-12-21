using CommunityToolkit.Maui.Extensions;
using Imapster.ContentViews;
using Imapster.Popups;
using MailKit;
using MimeKit;

namespace Imapster.ViewModels;

[DebuggerDisplay("Email: From = {From}, Date = {Date} '{Subject}'")]
public partial class EmailViewModel : ObservableObject, IDataGridItem, IEquatable<EmailViewModel>
{
    [ObservableProperty]
    private uint _id = default!;

    [ObservableProperty]
    private string _from = default!;

    [ObservableProperty]
    private string _to = default!;

    [ObservableProperty]
    private DateTime _date = default!;

    [ObservableProperty]
    private string _subject = default!;

    [ObservableProperty]
    private string _body = default!;

    [ObservableProperty]
    private bool _isRead = default!;

    [ObservableProperty]
    private string _folderId = default!;

    [ObservableProperty]
    private int _accountId;

    [ObservableProperty]
    private bool _hasAttachments;

    [ObservableProperty]
    private uint? _size;

    // AI Properties
    [ObservableProperty]
    private string? _aiSummary;

    [ObservableProperty]
    private string? _aiCategory;

    [ObservableProperty]
    private bool? _aiDelete;

    [ObservableProperty]
    private string? _aiDeleteMotivation;

    [ObservableProperty]
    private bool _isSelected;

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

    public EmailViewModel(uint id, string from, string to, DateTime date, string subject, string body, bool isRead, string folderId, int accountId, uint? size, bool hasAttachements)
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
        HasAttachments = hasAttachements;
    }

    public static EmailViewModel FromMessage(MimeMessage message, UniqueId id, string folderId, int accountId, uint? size, MessageFlags? flags)
    {
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
            message.Attachments?.Count() > 0
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
        nameof(HasAttachments) => HasAttachments,
        nameof(AiCategory) => AiCategory,
        nameof(AiDeleteMotivation) => AiDeleteMotivation,
        _ => null,
    };

    [RelayCommand]
    private async Task ShowDetailsAsync() =>
        await Shell.Current.ShowPopupAsync(new EmailDetailsPopup(this));

    public bool Equals(EmailViewModel? other) => 
        other is not null &&
        Id == other.Id &&
        FolderId == other.FolderId &&
        AccountId == other.AccountId;
}
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Services;
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

        return new EmailViewModel
        {
            Id = id.Id,
            From = message.From.ToString(),
            To = message.To.ToString(),
            Date = message.Date.DateTime,
            Subject = message.Subject ?? "-",
            Body = message.TextBody ?? message.HtmlBody ?? "",
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

    [RelayCommand]
    private async Task ShowDetailsAsync()
    {
        var popupService = App.Services.GetRequiredService<IPopupService>();

        var parameters = new Dictionary<string, object>
        {
            { "email", this }
        };

        await popupService.ShowPopupAsync<EmailDetailsViewModel>(
            Shell.Current,
            options: new PopupOptions { Shape = null, Shadow = null },
            shellParameters: parameters
        );
    }

    public bool Equals(EmailViewModel? other) =>
        other is not null &&
        Id == other.Id &&
        FolderId == other.FolderId &&
        AccountId == other.AccountId;
}
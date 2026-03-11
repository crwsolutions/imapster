using CommunityToolkit.Mvvm.ComponentModel;

namespace Imapster.ViewModels;

public partial class FolderViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Id { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int UnreadCount { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial int AccountId { get; set; }

    [ObservableProperty]
    public partial bool IsTrash { get; set; }
}
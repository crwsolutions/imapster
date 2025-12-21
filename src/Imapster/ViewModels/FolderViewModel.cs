using CommunityToolkit.Mvvm.ComponentModel;

namespace Imapster.ViewModels;

public partial class FolderViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int _unreadCount;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private int _accountId;

    [ObservableProperty]
    private bool _isTrash;
}
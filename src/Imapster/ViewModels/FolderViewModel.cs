namespace Imapster.ViewModels;


[DebuggerDisplay("FolderViewModel: {Id} '{Name}' Selected:{IsSelected} Children: {Children.Count}")]
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

    [ObservableProperty]
    public partial bool IsExpanded { get; set; } = true;

    [ObservableProperty]
    public partial ObservableCollection<FolderViewModel> Children { get; set; } = [];

    [ObservableProperty]
    public partial int IndentLevel { get; set; }

    [ObservableProperty]
    private bool _hasChildren;

    partial void OnHasChildrenChanged(bool value)
    {
        if (value)
        {
            IsExpanded = true;
        }
    }
}
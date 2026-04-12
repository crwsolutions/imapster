namespace Imapster.ViewModels;

public partial class AttachmentViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string FileName { get; set; } = string.Empty;
}
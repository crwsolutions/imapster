using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Imapster.ViewModels;

public partial class ImapAccountViewModel : BaseViewModel
{
    [ObservableProperty]
    public partial int Id { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Server { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int Port { get; set; }

    [ObservableProperty]
    public partial bool UseSsl { get; set; }

    [ObservableProperty]
    public partial string Username { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;
}
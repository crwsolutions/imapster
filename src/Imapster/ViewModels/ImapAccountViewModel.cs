using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Imapster.ViewModels;

public partial class ImapAccountViewModel : BaseViewModel
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _server = string.Empty;

    [ObservableProperty]
    private int _port;

    [ObservableProperty]
    private bool _useSsl;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;
}
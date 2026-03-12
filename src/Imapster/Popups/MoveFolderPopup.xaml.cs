using CommunityToolkit.Maui.Views;
using Imapster.Repositories;
using Imapster.ViewModels;

namespace Imapster.Popups;

public partial class MoveFolderPopup : Popup<bool>
{
    private readonly IFolderRepository _folderRepository;
    private MoveFolderPopupViewModel _viewModel;

    public MoveFolderPopup()
    {
        InitializeComponent();
        _folderRepository = new FolderRepository();
        _viewModel = new MoveFolderPopupViewModel(_folderRepository, 0, string.Empty);
        BindingContext = _viewModel;
    }
}
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Views;
using Imapster.Repositories;

namespace Imapster.Popups;

public partial class EditAccountPopup : Popup<bool>
{
    private readonly AccountRepository _accountRepository;
    private readonly IFolderPicker _folderPicker;
    private ImapAccountViewModel _viewModel;

    public EditAccountPopup(ImapAccountViewModel account, IFolderPicker folderPicker)
    {
        InitializeComponent();
        _accountRepository = new AccountRepository();
        _folderPicker = folderPicker;
        _viewModel = account;
        BindingContext = _viewModel;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        try
        {
            await _accountRepository.UpdateAccountAsync(_viewModel);
            await CloseAsync(true);
        }
        catch (Exception ex)
        {
            await Application.Current!.Windows[0]!.Page!.DisplayAlertAsync("Error", $"Failed to update account: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await CloseAsync(false);
    }

    private async void OnBrowseClicked(object? sender, EventArgs e)
    {
        try
        {
            // Use FolderPicker to select a folder
            var result = await _folderPicker.PickAsync(CancellationToken.None);
            result.EnsureSuccess();

            var vm = BindingContext as ImapAccountViewModel;
            if (vm != null)
            {
                vm.AttachmentArchivePath = result.Folder.Path;
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.Windows[0]!.Page!.DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }
}

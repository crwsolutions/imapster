using CommunityToolkit.Maui.Views;
using Imapster.Repositories;

namespace Imapster.Popups;

public partial class EditAccountPopup : Popup<bool>
{
    private readonly AccountRepository _accountRepository;
    private ImapAccountViewModel _viewModel;

    public EditAccountPopup(ImapAccountViewModel account)
    {
        InitializeComponent();
        _accountRepository = new AccountRepository();
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
}
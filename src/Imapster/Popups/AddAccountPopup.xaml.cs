using CommunityToolkit.Maui.Views;
using Imapster.Repositories;

namespace Imapster.Popups;

public partial class AddAccountPopup : Popup<bool>
{
    private readonly AccountRepository _accountRepository;
    private ImapAccountViewModel _viewModel;

    public AddAccountPopup()
    {
        InitializeComponent();
        _accountRepository = new AccountRepository();
        _viewModel = new ImapAccountViewModel();
        BindingContext = _viewModel;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        try
        {
            // Generate a new ID for the account
            _viewModel.Id = new Random().Next(1000, 9999);

            await _accountRepository.AddAccountAsync(_viewModel);
            await CloseAsync(true);
        }
        catch (Exception ex)
        {
            await Application.Current!.Windows[0]!.Page!.DisplayAlertAsync("Error", $"Failed to save account: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await CloseAsync(false);
    }
}
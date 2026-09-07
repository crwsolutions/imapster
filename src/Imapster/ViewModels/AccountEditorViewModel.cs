using CommunityToolkit.Maui.Storage;
using Imapster.Repositories;

namespace Imapster.ViewModels;

/// <summary>
/// Backing ViewModel for the Add/Edit account popups. Owns the account being
/// edited and the save/browse logic, so the popup views stay thin.
/// </summary>
public partial class AccountEditorViewModel : ObservableObject, IQueryAttributable
{
    private readonly IAccountRepository _accountRepository;
    private readonly IFolderPicker _folderPicker;

    [ObservableProperty]
    public partial ImapAccountViewModel Account { get; set; } = new();

    [ObservableProperty]
    public partial bool SaveSucceeded { get; set; }

    public AccountEditorViewModel(IAccountRepository accountRepository, IFolderPicker folderPicker)
    {
        _accountRepository = accountRepository;
        _folderPicker = folderPicker;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("account", out var accountObj) && accountObj is ImapAccountViewModel account)
        {
            Account = account;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var isAdd = Account.Id == 0;
        SaveSucceeded = false;

        try
        {
            if (isAdd)
            {
                Account.Id = new Random().Next(1000, 9999);
                await _accountRepository.AddAccountAsync(Account);
            }
            else
            {
                await _accountRepository.UpdateAccountAsync(Account);
            }

            SaveSucceeded = true;
        }
        catch (Exception ex)
        {
            var action = isAdd ? "save" : "update";
            await Application.Current!.Windows[0]!.Page!.DisplayAlertAsync(
                "Error", $"Failed to {action} account: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        try
        {
            var result = await _folderPicker.PickAsync(CancellationToken.None);
            result.EnsureSuccess();
            Account.AttachmentArchivePath = result.Folder.Path;
        }
        catch (Exception ex)
        {
            await Application.Current!.Windows[0]!.Page!.DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }
}

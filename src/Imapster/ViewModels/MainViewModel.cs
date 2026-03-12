using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using Imapster.Popups;
using Imapster.Repositories;
using Imapster.Services;

namespace Imapster.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly IImapSyncService _imapSyncService;
    private readonly IFolderRepository _folderRepository;
    private readonly IEmailRepository _emailRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly EmailAiService _emailAiService;
    private readonly IPopupService _popupService;

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Not connected";

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial FolderViewModel? SelectedFolder { get; set; }

    [ObservableProperty]
    public partial bool IsConnected { get; set; } = false;

    [ObservableProperty]
    public partial int RowCount { get; set; } = 0;

    private bool _isLoading = true;

    partial void OnSelectedFolderChanged(FolderViewModel? value)
    {
        if (value is null || _isLoading) return;

        _ = LoadEmailsForFolderAsync(value.Id);
    }

    [ObservableProperty]
    public partial ImapAccountViewModel? SelectedAccount { get; set; }

    partial void OnSelectedAccountChanged(ImapAccountViewModel? value)
    {
        // When account changes, reload folders and emails for that account
        if (value is null || _isLoading) return;
        
        SelectedAccount = value;
        SelectedFolder = null;
        Folders.Clear();
        _ = LoadFoldersAndEmailsFromLocaAsync();
    }

    public ObservableCollection<ImapAccountViewModel> Accounts { get; } = [];

    public ObservableCollection<FolderViewModel> Folders { get; } = [];

    [ObservableProperty]
    public partial ObservableCollection<EmailViewModel>? Emails { get; set; } = [];

    public MainViewModel(IImapSyncService imapSyncService,
                         IFolderRepository folderRepository,
                         IEmailRepository emailRepository,
                         IAccountRepository accountRepository,
                         EmailAiService emailAiService,
                         IPopupService popupService)
    {
        _imapSyncService = imapSyncService;
        _folderRepository = folderRepository;
        _emailRepository = emailRepository;
        _accountRepository = accountRepository;
        _emailAiService = emailAiService;
        _popupService = popupService;

        Title = "IMAP Client";

        // Load data from repositories
        LoadDataAsync();
    }

    private async void LoadDataAsync()
    {
        try
        {
            _isLoading = true;
            IsBusy = true;

            // Load accounts
            var accounts = await _accountRepository.GetAllAccountsAsync();
            foreach (var account in accounts)
            {
                Accounts.Add(account);
            }

            // If we have accounts, select the first one by default
            if (accounts.Count > 0)
            {
                SelectedAccount = accounts.First();
                await LoadFoldersAndEmailsFromLocaAsync();
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error loading data: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            IsBusy = false;
        }
    }

    private async Task LoadFoldersAndEmailsFromLocaAsync()
    {
        try
        {
            IsBusy = true;

            if (SelectedAccount is null)
            {
                StatusText = "No account selected";
                return;
            }

            Emails = [];

            // Load folders for the selected account from local storage
            if (SelectedFolder is null)
            {
                await LoadFoldersFromLocalAsync();

                if (SelectedFolder is null)
                { 
                    return;
                }
            }

            await LoadEmailsForFolderAsync(SelectedFolder.Id);
        }
        catch (Exception ex)
        {
            StatusText = $"Error loading data: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadFoldersFromLocalAsync()
    {
        if (SelectedAccount is null)
        {
            StatusText = "No account selected";
            return;
        }

        Folders.Clear();
        var folders = await _folderRepository.GetAllFoldersAsync(SelectedAccount.Id);
        foreach (var folder in folders)
        {
            Folders.Add(folder);
        }

        if (Folders?.Count > 0)
        {
            // Only set default folder if none is selected or it's not in the list anymore
            if (SelectedFolder == null || !Folders.Any(f => f.Id == SelectedFolder.Id))
            {
                SelectedFolder = Folders.FirstOrDefault(f => f.Name.ToLower() == "inbox") ?? Folders.First();

                StatusText = $"Selected folder '{SelectedFolder.Name}'";
            }
        }
    }

    private async Task LoadEmailsForFolderAsync(string id)
    {
        if (SelectedAccount is null)
        {
            StatusText = "No account selected";
            return;
        }

        IsBusy = true;
        StatusText = $"Getting local emails of '{id}'";

        // Load emails from local storage in background thread
        var emails = await Task.Run(() => _emailRepository.GetEmailsByFolderIdAsync(SelectedAccount.Id, id)).ConfigureAwait(false);
        var uiEmails = new ObservableCollection<EmailViewModel>();
        foreach (var email in emails)
        {
            uiEmails.Add(email);
        }

        // Update UI on main thread
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Emails = uiEmails;
        });

        StatusText = $"Retrieved {emails.Count} emails";

        IsBusy = false;
    }

    [RelayCommand]
    private async Task AddAccount()
    {
        var popup = new AddAccountPopup();
        var result = await Shell.Current.ShowPopupAsync<bool>(popup);
        
        if (result.Result is true)
        {
            // Reload accounts
            Accounts.Clear();
            var accounts = await _accountRepository.GetAllAccountsAsync();
            foreach (var account in accounts)
            {
                Accounts.Add(account);
            }
            
            StatusText = "Account added successfully";
        }
    }

    [RelayCommand]
    private async Task EditAccount()
    {
        if (SelectedAccount == null)
        {
            StatusText = "Please select an account to edit";
            return;
        }
        
        var popup = new EditAccountPopup(SelectedAccount);
        var result = await Shell.Current.ShowPopupAsync<bool>(popup);
        
        if (result.Result is true)
        {
            // Reload accounts
            Accounts.Clear();
            var accounts = await _accountRepository.GetAllAccountsAsync();
            foreach (var account in accounts)
            {
                Accounts.Add(account);
            }
            
            StatusText = "Account updated successfully";
        }
    }

    [RelayCommand]
    private async Task Connect()
    {
        if (SelectedAccount == null)
        {
            StatusText = "Please select an account first";
            return;
        }

        StatusText = "Connecting to server...";
        IsBusy = true;

        try
        {
            await _imapSyncService.ConnectAsync(SelectedAccount);

            // Sync folders from server to local storage
            await SyncFoldersAsync();

            // Sync emails from server to local storage (for selected folder)
            if (SelectedFolder != null)
            {
                await SyncEmailsAsync(SelectedFolder.Id);
            }

            // Reload folders and emails from local storage
            await LoadFoldersAndEmailsFromLocaAsync();

            IsConnected = true;
            StatusText = $"Connected to {SelectedAccount.Name}";
        }
        catch (Exception ex)
        {
            StatusText = $"Connection failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Disconnect()
    {
        _imapSyncService.Dispose();
        IsConnected = false;
        StatusText = "Disconnected";
    }

    [RelayCommand]
    private async Task Ai()
    {
        if (Emails is null)
        {
            return;
        }

        IsBusy = true;

        StatusText = "Connecting to ai...";

        int i = 0;

        try
        {
            foreach (var email in Emails)
            {
                if (string.IsNullOrWhiteSpace(email.AiSummary))
                {
                    i++;
                    try
                    {
                        var classification = await _emailAiService.ClassifyEmailAsync(email.ToMimeMessage());
                        email.AiSummary = classification.Summary;
                        email.AiCategory = classification.Category;
                        email.AiDelete = classification.Delete;
                        email.AiDeleteMotivation = classification.Reason;
                        await _emailRepository.UpdateEmailAsync(email);
                        StatusText = $"Generated summary for email '{email.Subject}' : Delete? -> {email.AiDelete}";
                    }
                    catch (Exception ex)
                    {
                        StatusText = $"Error generating summary: {ex.Message}";
                        return;
                    }
                }
            }
        }
        finally
        {
            IsBusy = false;
        }    

        StatusText = $"AI processed {i} emails.";
    }

    [RelayCommand]
    private async Task Refresh()
    {
        if (SelectedAccount == null)
        {
            StatusText = "Please connect to an account first";
            return;
        }

        StatusText = "Refreshing...";
        IsBusy = true;

        try
        {
            // Refresh folders from server to local storage
            await SyncFoldersAsync();

            // Refresh emails for the selected folder from server to local storage
            if (SelectedFolder != null)
            {
                await SyncEmailsAsync(SelectedFolder.Id);
            }

            // Reload folders and emails from local storage
            await LoadFoldersAndEmailsFromLocaAsync();

            StatusText = "Refreshed successfully";
        }
        catch (Exception ex)
        {
            StatusText = $"Refresh failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Trash()
    {
        if (!IsConnected)
        {
            StatusText = "Please connect to server first";
            return;
        }

        if (Emails is null || SelectedFolder == null)
        {
            StatusText = "No emails to trash";
            return;
        }

        // Get selected emails
        var selectedEmails = Emails.Where(e => e.IsSelected).ToList();
        if (!selectedEmails.Any())
        {
            StatusText = "No emails selected";
            return;
        }

        try
        {
            IsBusy = true;
            StatusText = $"Moving {selectedEmails.Count} emails to trash...";

            // Get email IDs for the selected emails
            var emailIds = selectedEmails.Select(e => e.Id).ToList();

            // Move emails to trash on server via IMAP service
            var result = await _imapSyncService.MoveEmailsToTrashAsync(SelectedFolder.Id, emailIds);

            // Remove from local collection (for fastest UI response)
            foreach (var email in selectedEmails.ToList()) // Use ToList() to avoid modification during enumeration
            {
                Emails.Remove(email);
            }

            // Update row count
            RowCount = Emails.Count;

            StatusText = $"Moved {selectedEmails.Count} emails to trash";
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to move emails to trash: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Move()
    {
        if (!IsConnected)
        {
            StatusText = "Please connect to server first";
            return;
        }

        if (Emails is null || SelectedFolder == null)
        {
            StatusText = "No emails to move";
            return;
        }

        var selectedEmails = Emails.Where(e => e.IsSelected).ToList();
        if (!selectedEmails.Any())
        {
            StatusText = "No emails selected";
            return;
        }

        try
        {
            IsBusy = true;

            var parameters = new Dictionary<string, object>
            {
                { "folderRepository", _folderRepository },
                { "accountId", SelectedAccount!.Id },
                { "sourceFolderId", SelectedFolder.Id }
            };

            var popupResult = await _popupService.ShowPopupAsync<MoveFolderPopupViewModel>(
                Shell.Current,
                options: new PopupOptions { Shape = null, Shadow = null },
                shellParameters: parameters
                );

            var pr = popupResult as IPopupResult<string>;

            if (pr?.Result is string result)
            {
                var emailIds = selectedEmails.Select(e => e.Id).ToList();
                var moveResult = await _imapSyncService.MoveEmailsToFolderAsync(SelectedFolder.Id, result, emailIds);

                foreach (var email in selectedEmails.ToList())
                {
                    Emails.Remove(email);
                }

                RowCount = Emails.Count;
                StatusText = moveResult;
            }
            else
            {
                StatusText = "Move cancelled";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to move emails: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EmptyTrash(FolderViewModel folder)
    {
        var page = Application.Current?.Windows[0].Page!;

        // Sanity check: ensure this is actually a trash folder
        if (folder?.IsTrash != true)
        {
            await page.DisplayAlertAsync("Invalid Operation", "This is not a trash folder.", "OK");
            return;
        }

        var confirmed = false;
        confirmed = await page.DisplayAlertAsync(
            "Empty Trash",
            "Are you sure you want to permanently delete all items in the trash folder?\nThis will remove emails from both local storage and the mail server.",
            "Yes", "No");

        if (!confirmed)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusText = "Emptying trash...";

            if (SelectedAccount != null)
            {
                // EmptyFolderAsync handles both remote and local deletion
                await _imapSyncService.EmptyFolderAsync(SelectedAccount.Id, folder.Id);
            }

            // Refresh the email list if trash folder is currently selected
            if (SelectedFolder?.Id == folder.Id)
            {
                Emails = [];
                RowCount = 0;
            }

            StatusText = "Trash emptied successfully";
        }
        catch (Exception ex)
        {
            StatusText = $"Error emptying trash: {ex.Message}";
            var errorPage = Application.Current?.Windows[0].Page;
            if (errorPage != null)
            {
                await errorPage.DisplayAlertAsync("Error", $"Failed to empty trash: {ex.Message}", "OK");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SyncFoldersAsync()
    {
        await _imapSyncService.FoldersAsync();
    }

    private async Task SyncEmailsAsync(string id)
    {
        await _imapSyncService.EmailsAsync(id);
    }
}

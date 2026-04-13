using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Storage;
using Imapster.ContentViews;
using Imapster.Extensions;
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
    private readonly IPromptRepository _promptRepository;
    private readonly IPopupService _popupService;
    private readonly IFolderPicker _folderPicker;

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
    private CancellationTokenSource? _aiCancellationTokenSource;

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

    [ObservableProperty]
    public partial ObservableCollection<IDataGridItem> DisplayedItems { get; set; } = [];

    public MainViewModel(IImapSyncService imapSyncService,
                          IFolderRepository folderRepository,
                          IEmailRepository emailRepository,
                          IAccountRepository accountRepository,
                          EmailAiService emailAiService,
                          IPromptRepository promptRepository,
                          IPopupService popupService,
                          IFolderPicker folderPicker)
    {
        _imapSyncService = imapSyncService;
        _folderRepository = folderRepository;
        _emailRepository = emailRepository;
        _accountRepository = accountRepository;
        _emailAiService = emailAiService;
        _promptRepository = promptRepository;
        _popupService = popupService;
        _folderPicker = folderPicker;

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

        // Build the folder hierarchy tree
        BuildFolderHierarchy(folders);

        if (Folders?.Count > 0)
        {
            // Only set default folder if none is selected or it's not in the list anymore
            if (SelectedFolder == null || !Folders.Any(f => f.Id == SelectedFolder.Id))
            {
                SelectedFolder = Folders.FirstOrDefault(f => string.Equals(f.Name, "inbox", StringComparison.OrdinalIgnoreCase)) ?? Folders.First();

                StatusText = $"Selected folder '{SelectedFolder.Name}'";
            }
        }
    }

    private void BuildFolderHierarchy(List<FolderViewModel> flatFolders)
    {
        Folders.Clear();
        var hierarchy = flatFolders.BuildHierarchy();

        foreach (var folder in hierarchy)
        {
            Folders.Add(folder);
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
        var popup = new AddAccountPopup(_folderPicker);
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

        var popup = new EditAccountPopup(SelectedAccount, _folderPicker);
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
        if (DisplayedItems is null || DisplayedItems.Count == 0)
        {
            return;
        }

        // Cancel any ongoing classification first
        _aiCancellationTokenSource?.Cancel();
        _aiCancellationTokenSource?.Dispose();

        IsBusy = true;
        _aiCancellationTokenSource = new CancellationTokenSource();

        StatusText = "Connecting to AI...";

        // Show cancel popup
        var popup = new CancelAiPopup(_aiCancellationTokenSource);
        Application.Current!.Windows[0].Page!.ShowPopup(popup, new PopupOptions
        {
            CanBeDismissedByTappingOutsideOfPopup = false
        });

        int i = 0;
        // Use DisplayedItems to only process visible emails
        var selectedEmails = DisplayedItems.Where(e => e.IsSelected).Cast<EmailViewModel>().ToList();
        if (selectedEmails.Count > 0)
        {
            StatusText = $"Processing {selectedEmails.Count} selected emails...";
        }
        else
        {
            // Only process new emails that are visible (filtered)
            selectedEmails = [.. DisplayedItems.Cast<EmailViewModel>().Where(e => string.IsNullOrWhiteSpace(e.AiSummary))];
            StatusText = $"Processing all new emails ({selectedEmails.Count} emails)...";
        }

        try
        {
            foreach (var email in selectedEmails)
            {
                _aiCancellationTokenSource.Token.ThrowIfCancellationRequested();

                i++;
                try
                {
                    var classification = await _emailAiService.ClassifyEmailAsync(email, _aiCancellationTokenSource.Token);
                    email.AiSummary = classification.Summary;
                    email.AiCategory = classification.Category;
                    email.AiDelete = classification.Delete;
                    email.AiDeleteMotivation = classification.Reason;
                    await _emailRepository.UpdateEmailAsync(email);
                    StatusText = $"Generated summary for email '{email.Subject}' : Delete? -> {email.AiDelete}";
                }
                catch (Exception ex)
                {
                    // Set error state and save the email
                    email.AiCategory = "Error";
                    email.AiSummary = ex.Message;
                    StatusText = $"Error generating summary for '{email.Subject}': {ex.Message}";
                }
            }
        }
        catch (OperationCanceledException)
        {
            StatusText = "AI classification cancelled";
        }
        finally
        {
            IsBusy = false;
            // Close the popup
            await popup.CloseAsync(false);
        }

        if (selectedEmails.Any())
        {
            StatusText = $"AI processed {i} selected emails.";
        }
        else
        {
            StatusText = $"AI processed {i} emails.";
        }
    }

    [RelayCommand]
    private void CancelAiClassification()
    {
        _aiCancellationTokenSource?.Cancel();
        StatusText = "AI classification cancelled";
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
    private async Task EditPrompt()
    {
        if (_promptRepository == null)
        {
            StatusText = "Prompt repository not available";
            return;
        }

        var parameters = new Dictionary<string, object>
        {
            { "promptRepository", _promptRepository }
        };

        await _popupService.ShowPopupAsync<PromptEditorPopupViewModel>(
            Shell.Current,
            options: new PopupOptions { Shape = null, Shadow = null },
            shellParameters: parameters
        );
    }

    [RelayCommand]
    private async Task Trash()
    {
        if (!IsConnected)
        {
            StatusText = "Please connect to server first";
            return;
        }

        if (DisplayedItems is null || DisplayedItems.Count == 0 || SelectedFolder == null)
        {
            StatusText = "No emails to trash";
            return;
        }

        // Get selected emails from displayed (visible) items only
        var selectedEmails = DisplayedItems.Where(e => e.IsSelected).Cast<EmailViewModel>().ToList();
        if (selectedEmails.Count == 0)
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
                Emails!.Remove(email);
            }

            // Update row count
            RowCount = DisplayedItems.Count;

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

        if (DisplayedItems is null || DisplayedItems.Count == 0 || SelectedFolder == null)
        {
            StatusText = "No emails to move";
            return;
        }

        // Get selected emails from displayed (visible) items only
        var selectedEmails = DisplayedItems.Where(e => e.IsSelected).Cast<EmailViewModel>().ToList();
        if (selectedEmails.Count == 0)
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
                    Emails!.Remove(email);
                }

                RowCount = DisplayedItems.Count;
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
        bool confirmed = await page.DisplayAlertAsync(
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

# Agent Guidelines for Imapster

## Project Overview
Imapster is a .NET 10 MAUI email management application that synchronizes with IMAP servers, provides AI-powered email categorization, and enables email organization through a user-friendly desktop interface.

## Build, Lint, and Test Commands

### Build Commands
```bash
dotnet build                          # Build the solution
dotnet build -c Release               # Build in Release mode
dotnet run                            # Run the application
```

### Test Commands
- No unit tests are configured for this project
- Manual testing and QA validation are the primary testing approach
- Run with: `dotnet run` and test features manually

### Linting
- Rely on .NET compiler warnings; project configured with `<NoWarn>1701;1702;MVVMTK0045</NoWarn>`
- Ensure code compiles without warnings
- Use Visual Studio or Rider for IDE-level linting

## Code Style and Conventions

### .NET and Language Version
- Target .NET 10 exclusively
- Use C# 12+ features (primary constructors, records, collection expressions)
- Enable nullable reference types (`<Nullable>enable</Nullable>`)
- Use `LangVersion>preview</LangVersion>` for MVVM Toolkit features

### Imports and Namespace Organization
- Use explicit `using` statements in files (no reliance solely on GlobalUsings.cs)
- Group imports: external packages first, then project namespaces
- Follow alphabetical order within groups
- Example:
  ```csharp
  using CommunityToolkit.Maui.Extensions;
  using Imapster.Repositories;
  using Imapster.Services;
  using Microsoft.Extensions.Logging;
  ```

### Naming Conventions
- **Classes/Interfaces**: PascalCase (e.g., `MainViewModel`, `IEmailRepository`)
- **Methods/Properties**: PascalCase (e.g., `LoadEmailsAsync`, `SelectedFolder`)
- **Local variables/parameters**: camelCase (e.g., `email`, `accountId`)
- **Private fields**: camelCase (e.g., `_logger`, `_folderRepository`)
- **Constants**: PascalCase (e.g., `DbPath`)
- **Async methods**: Suffix with `Async` (e.g., `ConnectAsync`, `SyncEmailsAsync`)
- **Interfaces**: Prefix with `I` (e.g., `IImapSyncService`, `IEmailRepository`)

### Code Organization
- Keep methods focused and concise (< 50 lines preferred)
- Place property declarations before method declarations
- Group related functionality together
- Use explicit namespace declarations (`namespace Imapster.ViewModels;`)

### Comments and Documentation
- Add comments only for complex logic or non-obvious behavior
- Avoid redundant comments that restate the code
- Use `///` XML documentation for public APIs
- Prefer self-documenting code through naming

### Error Handling
- Wrap service calls in try-catch blocks
- Log exceptions using `ILogger<T>` from Microsoft.Extensions.Logging
- Display user-friendly error messages via `DisplayAlertAsync()`
- Example:
  ```csharp
  try
  {
      await _imapSyncService.ConnectAsync(SelectedAccount);
  }
  catch (Exception ex)
  {
      _logger.LogError(ex, "Failed to connect to IMAP server");
      await DisplayAlertAsync("Error", "Connection failed", "OK");
  }
  ```

### Async/Await Usage
- All I/O operations (IMAP, network, database) must be asynchronous
- Use `async Task` for fire-and-forget operations
- Use `async Task<T>` for operations that return a value
- Avoid `.Result` or `.Wait()` to prevent deadlocks
- Use `ConfigureAwait(false)` for non-UI operations
- Use `MainThread.InvokeOnMainThreadAsync()` for UI updates

### .NET 10 MAUI API Updates
- **Animation Methods**: Use async versions (e.g., `FadeToAsync`) instead of obsolete synchronous methods like `FadeTo`
- **Application.Current.MainPage**: Obsolete. Use `Application.Current?.Windows[0].Page` instead
- **DisplayAlert()**: Use the async version `DisplayAlertAsync()` for non-blocking dialog operations
- **Always await** `DisplayAlertAsync()` calls to ensure proper async handling

## Architecture and Patterns

### MVVM Pattern
- **Views**: XAML files in `Views/` folder with minimal code-behind
- **ViewModels**: Classes in `ViewModels/` folder using CommunityToolkit.MVVM
- **Models**: Data classes (ViewModels used as models in this project)
- **Services**: Business logic in `Services/` folder
- **Repositories**: Data access in `Repositories/` folder with interfaces

### ViewModel Implementation
- Use `public partial class ViewModelName : ObservableObject`
- ✅ **CORRECT**: Use `[ObservableProperty]` on `public partial` auto-implemented properties only:
  ```csharp
  [ObservableProperty]
  public partial bool IsBusy { get; set; }
  ```
  The source generator creates a private backing field `_isBusy` automatically.
- ❌ **WRONG** (triggers MVVMTK0042): Never use `[ObservableProperty]` on private fields:
  ```csharp
  // DON'T DO THIS - triggers MVVMTK0042
  [ObservableProperty]
  private bool _isBusy;  // ❌
  ```
- ⚠️ **MVVMTK0042 Prevention**: The `[ObservableProperty]` attribute MUST only be applied to `public partial` auto-implemented properties (`{ get; set; }`). Never apply it to:
  - Private fields (e.g., `private bool _isBusy`)
  - Existing properties with manual backing fields
  - Non-partial classes
  - Properties without `{ get; set; }` accessors
- Use `[RelayCommand]` for command implementations
- Example:
  ```csharp
  public partial class EmailViewModel : ObservableObject
  {
      [ObservableProperty]
      public partial bool IsBusy { get; set; }

      [RelayCommand]
      private async Task LoadAsync() { }
  }
  ```

### Dependency Injection
- Register all services, repositories, and ViewModels in `MauiProgram.cs`
- Use constructor injection in ViewModels and Services
- Example pattern:
  ```csharp
  builder.Services
      .AddTransient<MainPage>()
      .AddTransient<MainViewModel>()
      .AddSingleton<IImapSyncService, ImapSyncService>()
      .AddSingleton<IEmailRepository, EmailRepository>();
  ```

### XAML Guidelines
- Use declarative XAML for all UI definitions
- Minimize code-behind; prefer data binding with `{Binding}`
- Use XAML converters from `Converters/` folder for value transformations
- Define reusable styles in `Resources/Styles/Styles.xaml`
- Define color constants in `Resources/Styles/Colors.xaml`
- Use `{StaticResource}` for static converters
- Use `Mode=OneTime` for properties that don't change after initial binding
- Use `Mode=TwoWay` for user input controls

### Data Binding and Commands
- Bind to commands via `Command="{Binding CommandName}"`
- Pass parameters using `CommandParameter="{Binding .}"`
- Use `ObservableCollection<T>` for collections that need UI updates

### Localization
- All user-visible text must be externalized to resource files
- Resource files in `Resources/Languages/AppResources.resx`
- In XAML, use `{lang:Text ResourceKey}` extension
- Resource keys follow PascalCase naming convention
- Supported languages: English and Dutch

## Project Structure
```
src/Imapster/
├── ContentViews/      # Reusable custom UI components (e.g., DataGridView)
├── Converters/        # XAML value converters for data binding
├── Popups/            # Popup dialogs and modals
├── Repositories/      # Data access layer with interfaces
├── Resources/
│   ├── Languages/     # Localization resource files (AppResources.resx)
│   └── Styles/        # XAML style definitions (Colors.xaml, Styles.xaml)
├── Services/          # Business logic and external service integrations
├── ViewModels/        # MVVM ViewModel classes
└── Views/             # MAUI Content Pages (UI layer)
```

### Control Usage
- **CollectionView**: For displaying collections (preferred over ListView)
- **Border**: For framed content (preferred over Frame)
- **Custom Controls**: Place in `ContentViews/` folder
- Use `HorizontalStackLayout` and `VerticalStackLayout` instead of StackLayout when possible
- Use `Grid` for complex layouts with multiple rows/columns

## Testing
- No unit tests are required for this .NET MAUI project
- Manual testing and QA validation are the primary testing approach

## Git and Version Control
- Repository: https://github.com/crwsolutions/imapster
- Main branch: `main`
- Commit messages should be clear and descriptive
- Follow conventional commits when applicable

## Common Patterns

### MainViewModel Pattern
- Manages account selection and connection state
- Coordinates folder and email list display
- Handles search and filtering operations
- Contains commands for user interactions (Connect, Disconnect, Refresh, etc.)
- Tracks IsBusy state for UI feedback

### Service Implementation
- Implement `IServiceName` interface with `ServiceName` class
- All I/O operations must be async using `async/await`
- Use `ILogger<T>` for logging
- Handle exceptions gracefully with user-friendly messages

### Repository Implementation
- Implement `IRepositoryName` interface
- Use Dapper for database queries
- All database operations must be async
- Use transactions for bulk operations

### Popups

Docs: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/maui/views/popup

**Use CommunityToolkit Popups**

Allowed APIs:

* `Popup` / `Popup<T>`
* `ShowPopupAsync(...)`
* `ClosePopupAsync()`
* `builder.Services.AddTransientPopup<TPopup, TViewModel>()`

Never use: `ShowPopup()` (non-async), `popup.Close()`, modal `ContentPage`, third-party popup libs.

**Showing a popup (anywhere, incl. Shell):**

```csharp
await this.ShowPopupAsync<NamePopup>();        // from a Page
await Shell.Current.ShowPopupAsync<NamePopup>(); // from Shell
```

**XAML popup / MVVM:**

```csharp
public partial class NamePopup : Popup<string>

<toolkit:Popup
    x:TypeArguments="x:String"
    x:DataType="vm:NamePopupViewModel">

builder.Services.AddTransientPopup<NamePopup, NamePopupViewModel>();

var result = await this.ShowPopupAsync<NamePopup>();
await ClosePopupAsync();
```

**Passing data to popup via IQueryAttributable:**

```csharp
// ViewModel - implement IQueryAttributable with [RelayCommand] for loading
public partial class MyPopupViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty]
    public partial ObservableCollection<FolderViewModel> AvailableFolders { get; set; } = [];

    private IFolderRepository? _folderRepository;
    private int _accountId;
    private string _sourceFolderId = string.Empty;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("folderRepository", out var repoObj) && repoObj is IFolderRepository folderRepository)
            _folderRepository = folderRepository;
        
        if (query.TryGetValue("accountId", out var accountIdObj) && accountIdObj is int accountId)
            _accountId = accountId;
            
        if (query.TryGetValue("sourceFolderId", out var sourceFolderIdObj) && sourceFolderIdObj is string sourceFolderId)
            _sourceFolderId = sourceFolderId;
    }

    [RelayCommand]
    private async Task LoadFoldersAsync()
    {
        var folders = await _folderRepository.GetAllFoldersAsync(_accountId);
        foreach (var folder in folders)
        {
            if (folder.Id != _sourceFolderId && !folder.IsTrash)
                AvailableFolders.Add(folder);
        }
    }
}

// Code-behind - parameterless constructor, trigger Load from Loaded event
public partial class MyPopup : Popup<string>
{
    public MyPopup()
    {
        InitializeComponent();
        this.Loaded += async (s, e) => await ((MyPopupViewModel)BindingContext!).LoadFoldersCommand.Execute(null);
    }
}

// MainViewModel - pass parameters via dictionary
var parameters = new Dictionary<string, object>
{
    { "folderRepository", _folderRepository },
    { "accountId", SelectedAccount.Id },
    { "sourceFolderId", SelectedFolder.Id }
};

var popup = new MyPopup();
var result = await Shell.Current.ShowPopupAsync(popup, parameters);
```

## Additional Notes
- This application is primarily a desktop email management tool
- Platform-specific code should use conditional compilation or platform handlers
- Always test across supported platforms before committing
- Keep performance in mind when loading and displaying large email lists
- Use `ConfigureAwait(false)` for background operations to improve performance
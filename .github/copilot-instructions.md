# Copilot Instructions for Imapster

## Project Overview
Imapster is a .NET 10 MAUI email management application that synchronizes with IMAP servers, provides AI-powered email categorization, and enables email organization and management through a user-friendly desktop interface.

## General Guidelines
- Target .NET 10 exclusively; do not use outdated .NET Framework or Xamarin Forms patterns.
- Use modern C# features (C# 12+) such as primary constructors, records, and LINQ methods.

### .NET 10 MAUI API Updates
- **Animation Methods**: Use async versions (e.g., `FadeToAsync`) instead of obsolete synchronous methods like `FadeTo`.
- **Application.Current.MainPage**: Obsolete. Use `Application.Current?.Windows[0].Page` instead.
- **DisplayAlert()**: Use the async version `DisplayAlertAsync()` for non-blocking dialog operations.
- Always await `DisplayAlertAsync()` calls to ensure proper async handling.

## Project Structure
The Imapster project is organized as follows:

```
src/Imapster/
+-- ContentViews/           # Reusable custom UI components
+-- Converters/             # XAML value converters for data binding
+-- DataModels/             # Data model classes
+-- Mappers/                # Object mapping logic between DTOs and ViewModels
+-- Repositories/           # Data access and persistence layer
+-- Resources/
¦   +-- Languages/          # Localization resource files (AppResources.resx)
¦   +-- Styles/             # XAML style definitions (Colors.xaml, Styles.xaml)
+-- Services/               # Business logic and external service integrations
+-- ViewModels/             # MVVM ViewModel classes
+-- Views/                  # MAUI Content Pages (UI layer)
+-- App.xaml                # Application root
+-- App.xaml.cs             # Application class
+-- MauiProgram.cs          # MAUI application configuration and DI setup
+-- GlobalUsings.cs         # Global using declarations
```

## Architecture and Patterns

### MVVM Pattern
Strictly follow the MVVM (Model-View-ViewModel) pattern:
- **Views**: XAML files in `Views/` folder with minimal code-behind (only wiring/event handlers if necessary)
- **ViewModels**: Classes in `ViewModels/` folder containing business logic, commands, and INotifyPropertyChanged properties
- **Models**: Plain data classes in `DataModels/` folder
- **Services**: Business logic in `Services/` folder, injected into ViewModels

### Dependency Injection
- Register all services, repositories, and ViewModels in `MauiProgram.cs`
- Use Microsoft.Extensions.DependencyInjection for DI container
- Example registration pattern:
  ```csharp
  builder.Services
      .AddTransient<MainPage>()
      .AddTransient<MainViewModel>()
      .AddSingleton<IImapSyncService, ImapSyncService>()
      .AddSingleton<IEmailRepository, EmailRepository>();
  ```

### Services Layer
- **ImapSyncService**: Handles IMAP protocol synchronization and email retrieval
- **EmailAiService**: Handles AI-powered email categorization and analysis
- **Other Services**: Follow the `IServiceName` / `ServiceName` naming convention
- All I/O operations (HTTP, IMAP, database) must be async using `async/await`
- Implement error handling with try-catch and logging

### Repositories
- Implement repository pattern for data access
- Use `IEmailRepository` interface for abstraction
- All database operations must be async
- Place in `Repositories/` folder with corresponding interface

## UI and Controls

### XAML Guidelines
- Use declarative XAML for all UI definitions
- Minimize code-behind; prefer data binding with `{Binding}`
- Use XAML converters from `Converters/` folder for value transformations
- Define all reusable styles in `Resources/Styles/Styles.xaml`
- Define color constants in `Resources/Styles/Colors.xaml`

### Control Usage
- **CollectionView**: For displaying collections (preferred over ListView)
- **Border**: For framed content (preferred over Frame)
- **Custom Controls**: Place in `ContentViews/` folder (e.g., `DataGridView`)
- Use `HorizontalStackLayout` and `VerticalStackLayout` for layout instead of StackLayout when possible
- Use `Grid` for complex layouts with multiple rows/columns

### Data Binding
- Use `Mode=OneTime` for properties that don't change after initial binding
- Use `Mode=TwoWay` for user input controls
- Use static converters via `{StaticResource}` notation
- Apply converters for type transformations:
  - `BoolToBoldConverter`: For conditional font attributes
  - `InvertedBoolConverter`: For inverted boolean visibility
  - `AiBackgroundConverter`, `AiForegroundConverter`: For AI category styling

### Commands
- Use `ICommand` implementations in ViewModels
- Bind to commands via `Command="{Binding CommandName}"`
- Pass parameters using `CommandParameter="{Binding .}"`

## Localization
- All user-visible text must be externalized to resource files
- Resource files located in `Resources/Languages/AppResources.resx` with localized variants
- In XAML, use the `{lang:Text ResourceKey}` extension for localized text
- Resource keys follow **PascalCase** naming convention
- Supported languages: English and Dutch
- Language switching handled by a centralized language service

Example XAML:
```xaml
<Button Text="{lang:Text Connect}" Command="{Binding ConnectCommand}" />
<Label Text="{lang:Text DisconnectedStatus}" />
```

## Services Implementation Guidelines

### Async/Await Usage
- All I/O operations (IMAP, network, database) must be asynchronous
- Use `async Task` for fire-and-forget operations with error handling
- Use `async Task<T>` for operations that return a value
- Avoid `.Result` or `.Wait()` to prevent deadlocks

### Error Handling
- Wrap service calls in try-catch blocks
- Log exceptions using `ILogger<T>` from Microsoft.Extensions.Logging
- Display user-friendly error messages via `Application.Current?.Windows[0].Page?.DisplayAlertAsync()`
- Example:
  ```csharp
  try
  {
      await _imapSyncService.SyncEmailsAsync();
  }
  catch (Exception ex)
  {
      _logger.LogError(ex, "Failed to sync emails");
      await Application.Current?.Windows[0].Page?.DisplayAlertAsync("Error", "Email sync failed", "OK");
  }
  ```

### Email Synchronization (ImapSyncService)
- Handle IMAP protocol operations asynchronously
- Sync emails from configured IMAP accounts
- Update local repository with fetched emails
- Handle connection state and timeouts gracefully

### AI Service (EmailAiService)
- Process emails for AI-powered categorization
- Return category classifications and deletion recommendations
- Handle API calls asynchronously with appropriate timeouts

## Code Style and Conventions

### Naming Conventions
- **Classes/Methods/Properties**: PascalCase
- **Local variables/parameters**: camelCase
- **Private fields**: camelCase (no underscore prefix required unless following existing convention)
- **Constants**: PascalCase or UPPER_CASE
- **Async methods**: Suffix with `Async` (e.g., `SyncEmailsAsync()`)

### Code Organization
- Keep methods focused and concise (< 50 lines preferred)
- Group related functionality together
- Use regions sparingly; prefer well-organized class structure
- Place property declarations before method declarations

### Comments
- Add comments only for complex logic or non-obvious behavior
- Avoid redundant comments that restate the code
- Use `///` XML documentation for public APIs

## ViewModel Implementation
Use the MVVM Community Toolkit pattern with source generators. The class must be `public partial` to enable compile-time property generation:
```csharp
public partial class EmailViewModel : ObservableObject
{
    [ObservableProperty]
    private string subject;

    [RelayCommand]
    private async Task LoadAsync()
    {
        // Implementation
    }
}
```
- **Class Declaration**: Always mark ViewModel classes as `public partial` to enable MVVM Community Toolkit source generators
- **[ObservableProperty]**: Decorate private fields; the toolkit generates public properties automatically
- **[RelayCommand]**: Use for command implementations; generates `ICommand` property with `Async` suffix appended automatically

## Testing
- No unit tests are required for this .NET MAUI project
- Manual testing and QA validation are the primary testing approach

## Build and Deployment
- Build target: .NET 10
- Project should compile without warnings
- Use `dotnet build` or Visual Studio build functionality
- Ensure all referenced NuGet packages are compatible with .NET 10

## Common Patterns in Imapster

### MainViewModel Pattern
- Manages account selection and connection state
- Coordinates folder and email list display
- Handles search and filtering operations
- Contains commands for user interactions (Connect, Disconnect, Refresh, etc.)
- Tracks IsBusy state for UI feedback

### Email Display
- Uses custom `DataGridView` control for tabular email display
- Displays email metadata: From, To, Date, Size, Subject
- Shows AI categorization with styled badges
- Indicates unread status with visual indicator
- Provides action buttons (Details, Delete, etc.)

### Folder Management
- Hierarchical folder display in `CollectionView`
- Context menu support for folder actions (e.g., Empty Trash)
- Trash folder identification and special handling

## Git and Version Control
- Repository: https://github.com/crwsolutions/imapster
- Main branch: `main`
- Commit messages should be clear and descriptive
- Follow conventional commits when applicable

## Additional Notes
- This application is primarily a desktop email management tool
- Platform-specific code (if needed) should use conditional compilation or platform handlers
- Always test across supported platforms before committing
- Keep performance in mind when loading and displaying large email lists

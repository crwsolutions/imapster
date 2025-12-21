---
description: .NET MAUI Coding Rules and Best Practices
---

Het .net Maui project bevind zich in de src\Imapster map. De projectstructuur is als volgt:

```
Projectstructuur
├── ContentViews/           # Herbruikbare UI componenten
├── Converters/             # XAML converters
├── DataModels/             # Model classes voor data
├── DtoToViewModelMappers/  # Mapping tussen DTO's en ViewModels, do not touch
├── Extensions/             # Extension methods
├── Mappers/                # Object mapping logica
├── Navigation/             # Navigatie logica
├── Platforms/              # Platform specifieke code
├── Popups/                 # Popup implementaties
├── Repositories/           # Data access logica
├── Resources/              # Resource (/ language) bestanden
├── Services/               # Business logica en services
├── ViewModels/             # ViewModel classes
├── ViewModelValidators/    # Validatie logica voor ViewModels
└── Views/                  # UI views (Pages)
GlobalUsings.cs             # Globale using statements
```

## Algemene Richtlijnen
- Gebruik altijd C# 12 of hoger voor moderne features zoals primary constructors.
- Volg de officiële .NET naming conventions: PascalCase voor klassen en methoden, camelCase voor variabelen en parameters.
- Houd code modulair en herbruikbaar; vermijd code-duplicatie door shared code in de core-laag te plaatsen.
- Gebruik async/await consequent voor alle I/O-operaties (zoals HTTP-calls of database-toegang) om UI-blokkering te voorkomen.
- Implementeer dependency injection (DI) met Microsoft.Extensions.DependencyInjection voor services en viewmodels.

## Architectuur en Patroon
- Gebruik het MVVM (Model-View-ViewModel) patroon strikt:
  - **Model**: Plain data classes of entities (bijv. met records voor immutable data).
  - **Page**: XAML voor UI, met code-behind alleen voor minimal wiring (geen business logic).
  - **ViewModel**: Bevat logica, commands (met ICommand) en INotifyPropertyChanged voor data-binding.
- Scheid verantwoordelijkheden: Plaats platform-specifieke code (bijv. voor Android/iOS/Windows) in conditional compilation (#if ANDROID) of handlers.
- Voor navigatie: Gebruik Shell of Routing met INavigationService in ViewModels, niet direct in Views.

## UI en Controls
- Gebruik XAML voor declaratieve UI; vermijd overmatig code-behind.
- Implementeer data-binding met {Binding} en converters waar nodig (bijv. BoolToVisibilityConverter, check of vraag of er niet een converter aanwezig is).
- Optimaliseer performance: Vermijd nested ScrollViews, CollectionView met virtualization.
- Styling: Definieer resources in Resources\Styles\Styles.xaml voor consistente theming (light/dark mode support).
- Registeren van pagina's en viewmodels: Gebruik AddViewsExtension.cs, voorbeeld: `builder.Services.AddTransientWithShellRoute<MainPage, MainViewModel>(nameof(MainPage));`
- Toevoegen van xaml aan het project

## Services en Data Access
- Database: Gebruik SQLite of Dapper; gebruik async methoden zoals await database.GetAsync<T>().
- Gebruik CommunityToolkit.Mvvm voor boilerplate (bijv. [ObservableProperty], [RelayCommand]) om code te vereenvoudigen.
- Error handling: Implementeer try-catch met logging (bijv. via Microsoft.Extensions.Logging) en toon user-friendly alerts met DisplayAlert.
- Gebruik [Dependency] voor dependency injection in ViewModels. Voorbeeld: 
```csharp
public partial class AppSettingsViewModel : ObservableObject
{
    [Dependency]
    private readonly LanguageService _languageService;

    [Dependency]
    private readonly ThemeService _themeService;
}
```

## Logging en Telemetrie
- Gebruik **Microsoft.Extensions.Logging** voor centrale logging; vermijd `Console.WriteLine`.
- Voorbeeld:
```csharp
public class SampleService
{
    [Dependency]
    private readonly ILogger<SampleService> _logger;

    public void DoWork()
    {
        _logger.LogInformation("Werk gestart op {time}", DateTime.UtcNow);
    }
}
```

## Obsolete
- Gebruik **CollectionView** in plaats van `ListView`
- Gebruik **Border** in plaats van `Frame`

## Testing en Deployment
- Voor .Net maui schrijven we geen tests

## Lokalisatie

De applicatie ondersteunt de talen Nederlands en Engels.

- Resourcebestanden bevinden zich in `Resources/Languages/AppResources.resx` en de gelokaliseerde versies daarvan.
- Gebruik in XAML de volgende notatie voor tekst: `{lang:Text About}`, waarbij `"About"` de resource-sleutel is.
- Resource-sleutels dienen de **PascalCase**-naamgevingsconventie te volgen.
- Taalwisseling wordt afgehandeld door de `LanguageService`.
- Alle tekst die zichtbaar is voor de gebruiker **moet** worden geëxternaliseerd in resourcebestanden.

### Voorbeeldgebruik in XAML
```xaml
<Label Text="{lang:Text About}" />
<Label Text="{lang:Text Welcome}" />
```

## Voorbeelden
### Voorbeeld ViewModel
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string name;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            // Simuleer async call
            Name = await SomeService.GetNameAsync();
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}

Voorbeeld XAML Viewxml

<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             x:Class="Imapster.Views.MainPage">
    <VerticalStackLayout>
        <Entry Text="{Binding Name}" />
        <Button Text="{lang:Text Load}" Command="{Binding LoadDataCommand}" />
    </VerticalStackLayout>
</ContentPage>


Wanneer je code genereert voor .NET MAUI:
- Begin altijd met het MVVM-patroon.
- Voeg comments toe voor complexe logica.
- Controleer op memory leaks (bijv. unsubscribe events).
- Zorg voor cross-platform compatibiliteit; specificeer platform-specifieke aanpassingen indien nodig.
- Houd methoden kort (< 50 regels) en refactor indien langer.

Gebruik deze rules om consistente, maintainable .NET MAUI-apps te bouwen.

using Imapster.Repositories;
using Microsoft.Extensions.Logging;

namespace Imapster.ViewModels;

public partial class PromptEditorPopupViewModel : ObservableObject, IPopupResult
{
    private readonly ILogger<PromptEditorPopupViewModel> _logger;
    private readonly IPromptRepository _promptRepository;

    [ObservableProperty]
    private string _verwijderRegels = DefaultVerwijderRegels;

    [ObservableProperty]
    private string _behoudenRegels = DefaultBehoudenRegels;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _staticIntro = StaticIntro;

    [ObservableProperty]
    private string _staticOutputFormat = StaticOutputFormat;

    public object? Result { get; set; }

    public PromptEditorPopupViewModel(ILogger<PromptEditorPopupViewModel> logger, IPromptRepository promptRepository)
    {
        _logger = logger;
        _promptRepository = promptRepository;
    }

    [RelayCommand]
    private async Task LoadRulesAsync()
    {
        IsBusy = true;
        try
        {
            var verwijderPrompt = await _promptRepository.GetVerwijderRegelsAsync();
            if (verwijderPrompt != null && !string.IsNullOrWhiteSpace(verwijderPrompt.Prompt))
            {
                VerwijderRegels = verwijderPrompt.Prompt;
            }

            var behoudenPrompt = await _promptRepository.GetBehoudenRegelsAsync();
            if (behoudenPrompt != null && !string.IsNullOrWhiteSpace(behoudenPrompt.Prompt))
            {
                BehoudenRegels = behoudenPrompt.Prompt;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading rules");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsBusy = true;
        try
        {
            var verwijderPrompt = new PromptTemplate
            {
                Prompt = VerwijderRegels
            };

            var behoudenPrompt = new PromptTemplate
            {
                Prompt = BehoudenRegels
            };

            await _promptRepository.SaveRulesAsync(verwijderPrompt, behoudenPrompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving rules");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ResetToDefaultAsync()
    {
        VerwijderRegels = DefaultVerwijderRegels;
        BehoudenRegels = DefaultBehoudenRegels;
    }

    private const string StaticIntro =
        """
        Je bent een assistent die helpt bij het opschonen van e-mail.

        Je beoordeelt een e-mail op **relevantie op lange termijn** voor de gebruiker.

        De gebruiker wil **oud nieuws, reclame, promoties en niet-actuele informatie verwijderen**,
        en **persoonlijke, belangrijke of later nog bruikbare e-mails behouden**.

        Je krijgt telkens:

        * Afzender
        * Titel
        * Inhoud

        ### Taken:

        1. Geef een **zeer korte samenvatting** (maximaal 1–2 zinnen).
        2. Geef een **advies**: `BEHOUDEN` of `VERWIJDEREN`.
        3. Geef een **korte motivatie** (1 zin).

        ### Beoordelingsregels:
        """;

    private const string DefaultVerwijderRegels =
        """
        # **VERWIJDEREN** als het gaat om:
          * reclame, aanbiedingen, nieuwsbrieven
          * tijdgebonden nieuws of aankondigingen
          * marketing, sales, events, reminders zonder blijvende waarde
          * DHL of PostNL Notificaties
          * bevestiging van bestelling, tenzij er ook een factuur is in of bijgevoegd
        """;

    private const string DefaultBehoudenRegels =
        """
        # **BEHOUDEN** als het gaat om:
          * persoonlijke communicatie
          * werk, afspraken, contracten, facturen, bevestigingen
          * informatie die later nog nuttig kan zijn
        """;

    private const string StaticOutputFormat =
        """

        ### Output-formaat (STRICT JSON, RFC 8259 compliant! ALLEEN JSON, GEEN uitleg of codeblokken):

        Geef één JSON-object terug met de volgende velden:

        - summary: korte samenvatting van de e-mail (string)
        - category: een van de volgende waarden: Persoonlijk, Werk, Administratie, Reclame, Nieuws, Overig
        - delete: true als de e-mail verwijderd moet worden, false als deze bewaard kan blijven
        - reason: korte reden voor de classificatie (string)
        - extra: optionele extra informatie (string of null)

        **Belangrijk:**
        - Gebruik altijd de exacte veldnamen zoals hierboven.
        - Gebruik alleen geldige JSON (dubbele aanhalingstekens voor strings, geen trailing commas, null indien leeg).
        - Nooit extra tekst of uitleg buiten het JSON-object plaatsen.

        **Voorbeeldoutput:**

        {
          "summary": "Je hebt een gratis iPhone gewonnen!",
          "category": "Reclame",
          "delete": true,
          "reason": "Het belooft onrealistische beloningen en komt van een onbekende afzender.",
          "extra": null
        }

        Wees beslissend. Vermijd twijfelwoorden zoals "misschien".
        """;
}
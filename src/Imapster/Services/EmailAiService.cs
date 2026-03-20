using Imapster.Repositories;
using Imapster.ViewModels;
using Microsoft.Extensions.AI;
using MimeKit;

using System.Text;
using System.Text.Json;

namespace Imapster.Services;

public record EmailClassificationResult(
    string Summary,
    string Category,
    bool Delete,
    string Reason,
    string? Extra);

public sealed class EmailAiService
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal static readonly string StaticIntro =
        """
        Je bent een assistent die helpt bij het opschonen van e-mail.

        Je beoordeelt een e-mail op **relevantie op lange termijn** voor de gebruiker.

        De gebruiker wil **oud nieuws, reclame, promoties en niet-actuele informatie verwijderen**,
        en **persoonlijke, belangrijke of later nog bruikbare e-mails behouden**.

        Je krijgt telkens:

        * Afzender
        * Titel
        * Datum inclusief leeftijd (nieuw, oud, zeer oud)
        * Leesstatus (gelezen/ongelezen)
        * Bijlagen
        * Inhoud

        ### Taken:

        1. Geef een **zeer korte samenvatting** (maximaal 1–2 zinnen).
        2. Geef een **advies**: `BEHOUDEN` of `VERWIJDEREN`.
        3. Geef een **korte motivatie** (1 zin).
        """;

    internal static readonly string DefaultVerwijderRegels =
        """
          * reclame, aanbiedingen, nieuwsbrieven
          * tijdgebonden nieuws of aankondigingen
          * marketing, sales, events, reminders zonder blijvende waarde
          * DHL of PostNL Notificaties
          * bevestiging van bestelling, tenzij er ook een factuur is in of bijgevoegd
        """;

    internal static readonly string DefaultBehoudenRegels =
        """
          * persoonlijke communicatie
          * werk, afspraken, contracten, facturen, bevestigingen
          * informatie die later nog nuttig kan zijn
        """;

    internal static readonly string StaticOutputFormat =
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

    private readonly IChatClient _chatClient;
    private readonly IPromptRepository _promptRepository;

    public EmailAiService(IChatClient chatClient, IPromptRepository promptRepository)
    {
        _chatClient = chatClient;
        _promptRepository = promptRepository;
    }

    public async Task<EmailClassificationResult> ClassifyEmailAsync(EmailViewModel email, CancellationToken cancellationToken = default)
    {
        var systemPrompt = await GetEffectivePromptAsync();
        
        List<ChatMessage> chatHistory = [];
        chatHistory.Add(new(ChatRole.System, systemPrompt));
        chatHistory.Add(new(ChatRole.User, GetMessage(email)));
        var options = new ChatOptions { };

        var bob = new StringBuilder();

        try
        {
            await foreach (var chunk in _chatClient.GetStreamingResponseAsync(chatHistory, options, cancellationToken))
            {
                if (!string.IsNullOrEmpty(chunk.Text))
                {
                    bob.Append(chunk.Text);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw; // Re-throw to allow caller to handle cancellation
        }

        var s = bob.ToString();
        var result = JsonSerializer.Deserialize<EmailClassificationResult>(s, _jsonOptions)!;
        return result;
    }

    private async Task<string> GetEffectivePromptAsync()
    {
        var verwijderPrompt = await _promptRepository.GetVerwijderRegelsAsync();
        var behoudenPrompt = await _promptRepository.GetBehoudenRegelsAsync();

        return $"""
            {StaticIntro}
            
            ### Beoordelingsregels:

            # **VERWIJDEREN** als het gaat om:
            {verwijderPrompt.Prompt}
            
            # **BEHOUDEN** als het gaat om:
            {behoudenPrompt.Prompt}
            
            {StaticOutputFormat}
            """;
    }

    static string GetMessage(EmailViewModel email)
    {
        var attachmentsInfo = !string.IsNullOrWhiteSpace(email.Attachments)
            ? $"Attachments: {email.Attachments}"
            : "No attachments";
            
        var ageInfo = GetAgeInfo(email.Date);
        var classification = ClassifyEmailAge(email.Date);

        return $"""
                Afzender: {email.From}
                To: {email.To}
                Titel: {email.Subject}
                Datum: {email.Date:yyyy-MM-dd} ({ageInfo})
                Datum classificatie: {classification}
                Lees Status: {(email.IsRead ? "gelezen" : "ongelezen")}
                Bijlagen: {attachmentsInfo}

                Inhoud:
                {email.Body}
                """;
    }

    private static string GetAgeInfo(DateTime emailDate)
    {
        var now = DateTime.Now;
        var diff = now - emailDate;
        
        var years = diff.TotalDays / 365.25;
        var months = diff.TotalDays / 30.44;
        
        if (years >= 1)
        {
            var fullYears = (int)years;
            var remainingMonths = (int)((months - fullYears * 12) % 12);
            return $"{fullYears} year{(fullYears != 1 ? "s" : "")}, {remainingMonths} month{(remainingMonths != 1 ? "s" : "")}";
        }
        else if (months >= 1)
        {
            var fullMonths = (int)months;
            return $"{fullMonths} month{(fullMonths != 1 ? "s" : "")}";
        }
        else
        {
            var days = diff.TotalDays;
            return $"{(int)days} day{(days != 1 ? "s" : "")}";
        }
    }

    private static string ClassifyEmailAge(DateTime emailDate)
    {
        var now = DateTime.Now;
        var diff = now - emailDate;
        var months = (int)(diff.TotalDays / 30.44);
        
        return months switch
        {
            < 3 => "nieuw",
            >= 3 and < 12 => "oud",
            _ => "zeer oud"
        };
    }
}
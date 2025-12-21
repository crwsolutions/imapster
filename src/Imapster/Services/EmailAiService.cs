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

    private readonly IChatClient _chatClient;

    public EmailAiService(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<EmailClassificationResult> ClassifyEmailAsync(MimeMessage message)
    {
        List<ChatMessage> chatHistory = [];
        chatHistory.Add(new(ChatRole.System, _systemPrompt));
        chatHistory.Add(new(ChatRole.User, GetMessage(message)));
        var options = new ChatOptions { };

        var bob = new StringBuilder();

        await foreach (var chunk in _chatClient.GetStreamingResponseAsync(chatHistory, options, CancellationToken.None))
        {
            if (!string.IsNullOrEmpty(chunk.Text))
            {
                bob.Append(chunk.Text);
            }
        }

        var s = bob.ToString();
        var result = JsonSerializer.Deserialize<EmailClassificationResult>(s, _jsonOptions)!;
        return result;
    }

    static string GetMessage(MimeMessage message)
    {
        return $"""
                From: {message.From}
                To: {message.To}
                Subject: {message.Subject ?? string.Empty}
                Date: {message.Date:F}

                Body (text preview):
                {GetBodyPreview(message, 4500)}
                """;
    }

    static string GetBodyPreview(MimeMessage message, int length)
    {
        var text = message.TextBody;

        if (string.IsNullOrWhiteSpace(text))
            text = message.HtmlBody;

        if (string.IsNullOrWhiteSpace(text))
            return "<no body>";

        text = text.Replace("\r", " ").Replace("\n", " ");

        return text.Length > length
            ? text.Substring(0, length) + "..."
            : text;
    }

    const string _systemPrompt =
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

        # **VERWIJDEREN** als het gaat om:
          * reclame, aanbiedingen, nieuwsbrieven
          * tijdgebonden nieuws of aankondigingen
          * marketing, sales, events, reminders zonder blijvende waarde
          * DHL of PostNL Notificaties
          * bevestiging van bestelling, tenzij er ook een factuur is in of bijgevoegd

        # **BEHOUDEN** als het gaat om:
          * persoonlijke communicatie
          * werk, afspraken, contracten, facturen, bevestigingen
          * informatie die later nog nuttig kan zijn

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

        Wees beslissend. Vermijd twijfelwoorden zoals “misschien”.
        """;

}

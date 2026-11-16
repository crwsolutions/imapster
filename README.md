# Imapster

Imapster is an IMAP email client for desktop (and mobile) built with [.NET MAUI 11](https://learn.microsoft.com/dotnet/maui). It syncs mailboxes from your IMAP accounts, categorizes incoming mail with a local AI model, and lets you organize, search and manage your emails.

## Features

- **IMAP sync** – connect one or more IMAP accounts and keep local copies of your folders up to date ([MailKit](https://github.com/jstedfast/MailKit))
- **AI categorization** – classify emails with a local model (llama.cpp / Ollama compatible) through [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/)
- **HTML email rendering** – email bodies are parsed and laid out by the bundled [Imapster.HtmlViewer](src/Imapster.HtmlViewer) and rendered with [SkiaSharp](https://skia.org/)
- **Full email management** – read, reply, move, archive and delete mail; search and filter; attachments
- **Localization** – English and Dutch

## Technology stack

| Area | Technology |
|------|------------|
| Framework | .NET 11, .NET MAUI 11 (MVVM with [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/MVVM)) |
| Mail | MailKit (IMAP) |
| AI | Microsoft.Extensions.AI (OpenAI-compatible clients, llama.cpp / Ollama) |
| Persistence | SQLite via Microsoft.Data.Sqlite and [Dapper](https://github.com/DapperLib/Dapper) |
| Rendering | SkiaSharp, AngleSharp, ReverseMarkdown (Imapster.HtmlViewer) |
| Testing | xUnit v3 / Microsoft Testing Platform |

## Repository layout

```
src/
  Imapster/                    # The MAUI mail client (app)
  Imapster.HtmlViewer/         # HTML parsing, layout and SkiaSharp rendering (class library)
  Imapster.HtmlViewer.Tests/   # Unit tests for the HTML viewer (xUnit v3)
  Imapster.HtmlViewer.Demo/    # Demo app for the HTML viewer
```

The main project follows the MAUI folder conventions: `Views/` (XAML pages), `ViewModels/`, `DataModels/` & `Models/`, `Services/`, `Repositories/`, `Converters/`, `ContentViews/` and `Resources/`.

## Prerequisites

- [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11) (11.0.100 or later; pinned in [global.json](global.json))
- Windows 10 19041+ (Windows build); optional macOS for MacCatalyst and Linux for Android builds

## Build

```powershell
dotnet build Imapster.slnx
```

## Run

```powershell
dotnet run --project src/Imapster/Imapster.csproj
```

For the HTML viewer demo:

```powershell
dotnet run --project src/Imapster.HtmlViewer.Demo/Imapster.HtmlViewer.Demo.csproj
```

## Tests

The HTML viewer test project uses the Microsoft Testing Platform (xUnit v3). The runner is selected in [global.json](global.json):

```powershell
dotnet test --project src/Imapster.HtmlViewer.Tests/Imapster.HtmlViewer.Tests.csproj
```

## Configuration

IMAP accounts are added from the app itself (server, port, user, SSL). The AI endpoint for categorization is configured in [src/Imapster/MauiProgram.cs](src/Imapster/MauiProgram.cs) – point it at any OpenAI-compatible API (llama.cpp server or Ollama).

## License

MIT – see [LICENSE](LICENSE).

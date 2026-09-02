# Html to Markdown conversion for email classification

- Status: Complete
- Plan file: `.alta/plans/2026-09-02-html-to-markdown-classification.md`
- Created: 2026-09-02
- Task: Add a `ToMarkdown` conversion to the Imapster.HtmlViewer library (separate class) and use it in `EmailAiService.ClassifyEmailAsync` so the AI classifies compact Markdown instead of raw HTML.
- Git: not ignored (`.alta/plans/` is tracked) — commit this plan file with the related implementation work.

## Objective
- Convert email HTML bodies to Markdown **before** sending them to the AI for classification, to reduce input size and (thereby) classification latency/cost.
- Non-goals: no changes to the email display pipeline (HtmlViewer rendering), no changes to the classification prompt rules, no truncation of content.

## Context and evidence
- `src/Imapster.HtmlViewer/Imapster.HtmlViewer.csproj` — `ReverseMarkdown 6.2.1` already added (uncommitted working-tree change; keep it as-is, including the added BOM).
- `ReverseMarkdown 6.2.1` API verified against the restored NuGet package (reflection on `lib/net10.0/ReverseMarkdown.dll`):
  - `Converter` ctors: `Converter()`, `Converter(Config)`, `Converter(Config, IBrowsingContext)`; methods `Convert(string html)`, `Preprocess(string html)`.
  - `Config.Flavor` = `MarkdownFlavor.GitHub`; `Config.GithubFlavored` bool alias.
  - `Config.Preprocess` (`ReverseMarkdown.Preprocessing.HtmlPreprocessor`) chaining steps: `RemoveScripts()`, `RemoveStyles()`, `RemoveStyleSheets()`, `RemoveComments()`, `RemoveHidden()`, `RemoveClasses(selector)`, `RemoveAttributes(selector, params names)`, `RemoveEmptyElements(selector = "p, div, span")`, `ConvertInlineStylesToTags()`, `Unwrap(selector)`, `Rename(selector, tagName)`, `ReplaceWith(selector, html)`, `ResolveRelativeUrls(baseUrl)`.
  - `Config.Tables.CellListHandling = Config.TableCellListHandlingOption.InlineText` — flattens lists inside table cells to inline text so **no raw HTML survives** in the output (docs recommend this for LLM/RAG input).
- `src/Imapster/Services/EmailAiService.cs:103` `ClassifyEmailAsync` → `GetMessage(email)` (line 195) embeds `{email.Body}` under `Inhoud:`. `EmailViewModel.Body` (`src/Imapster/ViewModels/EmailViewModel.cs:64`) is the raw HTML body; `BodyHtml` (line 66) only wraps plain text in `<pre>` when no HTML is detected.
- `src/Imapster/Imapster.csproj:85` already references `Imapster.HtmlViewer` → no new project reference needed.
- `src/Imapster.HtmlViewer.Tests/Imapster.HtmlViewer.Tests.csproj` — xunit v3 (3.2.2), `net10.0-windows10.0.19041.0`, project-references `Imapster.HtmlViewer`; existing tests use plain `[Fact]` classes (e.g. `BaselineAndDecorationTests.cs`).

## Assumptions and open decisions
- **Resolved (2026-09-02, Colin approved "Zoals gepland"):** class name `HtmlMarkdownConverter` (namespace `Imapster.HtmlViewer`), new file `src/Imapster.HtmlViewer/HtmlMarkdownConverter.cs` at the library root.
- **Resolved (2026-09-02):** `ToMarkdown` is a `public static string ToMarkdown(string html)` — stateless, no DI changes.
- Output flavor: GitHub Markdown (nice tables) + `CellListHandling = InlineText` (lossy for lists in table cells, but explicitly recommended for "Markdown that is read rather than rendered"). Accepted trade-off for classification input.
- The `ReverseMarkdown` package line in the csproj stays as committed-by-you (including the BOM change on the `<Project>` line); do not "fix" it.

## Design notes
- New file `src/Imapster.HtmlViewer/HtmlMarkdownConverter.cs`:
  - `public static class HtmlMarkdownConverter` with:
    - `public static string ToMarkdown(string html)` — guards null/whitespace (returns `""`), builds the shared `Config`, calls `new Converter(config).Convert(html)`, and writes `Debug.WriteLine($"HtmlToMarkdown: {beforeBytes} bytes HTML -> {afterBytes} bytes markdown")` with UTF-8 byte counts (`Encoding.UTF8.GetByteCount`), before/after as Colin requested.
    - `internal static Config BuildConfig()` (internal so tests can inspect/reuse if needed) — the preprocessing pipeline per https://mysticmind.github.io/reversemarkdown-net/preprocessing, ordered:
      1. `RemoveScripts()` — `<script>`/`<noscript>` + inline `on*` handlers
      2. `RemoveStyles()` — inline `style` attrs, `<style>` elements, stylesheet `<link>` (biggest byte win in email HTML)
      3. `RemoveComments()`
      4. `RemoveHidden()` — `display:none` / `visibility:hidden` / `hidden` (email preheader spam)
      5. `ConvertInlineStylesToTags()` — Word/Outlook/Google-Docs `font-weight:700` → `<strong>`, italic → `<em>`, strikethrough → `<del>`
      6. `Unwrap("span, font")` — shed the presentational wrappers
      7. `RemoveClasses(":not(pre):not(code)")`
      8. `RemoveAttributes("*", "data-*", "id")`
      9. `RemoveEmptyElements()`
    - Config: `Flavor = MarkdownFlavor.GitHub` (via `GithubFlavored = true`), `Tables.CellListHandling = InlineText`.
  - `using`s: `System.Text`, `System.Diagnostics`, `ReverseMarkdown` (explicit, per AGENTS.md).
- `src/Imapster/Services/EmailAiService.cs`:
  - In `GetMessage` (line 204-215), replace `{email.Body}` with `{HtmlMarkdownConverter.ToMarkdown(email.Body)}`. `ClassifyEmailAsync` itself is unchanged — this is the function's input-message builder and satisfies "voeg de conversie toe aan ClassifyEmailAsync". The `Debug.WriteLine` byte counts fire once per classification, exactly there.
  - `using Imapster.HtmlViewer;` added (grouped with project namespaces).
- Rejected alternative: a DI-registered instance service in the main app — more churn for zero benefit; the converter is stateless.

## Risks and challenges
- Slight quality shift in what the AI sees (markdown instead of HTML) — headings/lists become semantically clearer, but presentational nuance is gone; this is the intended trade-off.
- `InlineText` cell-list flattening is lossy (nested lists in table cells collapse to one level) — acceptable for classification.
- ReverseMarkdown `Convert` is synchronous (parses via AngleSharp); for very large emails this adds a few ms on the UI thread's async path only during classification (already awaited in a background context by the callers at `EmailViewModel.cs:257` and `MainViewModel.cs:384`).
- `RemoveStyles()` before `ConvertInlineStylesToTags()` would break the bold/italic promotion — order matters; step 5 must run before the spans are unwrapped and style attributes are gone (they are consumed in step 5; step 2 only removes `style` attrs on *other* elements — verified pipeline semantics: `ConvertInlineStylesToTags` strips the declarations it consumes, so running `RemoveStyles` first is fine too, but we keep the documented order: scripts → styles → comments → hidden → promote → unwrap → classes/attrs → empty).
  - Note for builder: if the bold-promotion test fails, swap step 2 and step 5 (promote before `RemoveStyles`).

## Implementation checklist
- [x] Create `src/Imapster.HtmlViewer/HtmlMarkdownConverter.cs` — `HtmlMarkdownConverter.ToMarkdown(string)` + `BuildConfig()` with the preprocessing pipeline, `GithubFlavored = true`, `Tables.CellListHandling = InlineText`, UTF-8 byte-count `Debug.WriteLine` before/after.
- [x] Modify `src/Imapster/Services/EmailAiService.cs` — add `using Imapster.HtmlViewer;`; in `GetMessage` send `HtmlMarkdownConverter.ToMarkdown(email.Body)` as `Inhoud` instead of the raw `email.Body`.
- [x] Create `src/Imapster.HtmlViewer.Tests/HtmlMarkdownConverterTests.cs` (xunit v3 `[Fact]`s) — 10 tests, all passing:
  - [x] Basic: `<strong>` + `<a>` → `**paragraph**` and `[my site](http://test.com)`.
  - [x] Unordered list converted (`<li>`/`<ul>` gone, items present).
  - [x] Hidden `display:none` preheader removed, visible text kept.
  - [x] Scripts + `<style>` + inline styles removed, body kept.
  - [x] Inline-style promotion: `font-weight:700` → `**bold**`, `font-style:italic` → `*italic*`.
  - [x] Presentational wrappers (`span`/`font`) shed.
  - [x] Table cell list stays readable (no `<ol`/`<li` markup).
  - [x] Tracking attrs stripped (`data-*`, `id`, `class`).
  - [x] Large payload strictly smaller (UTF-8 bytes).
  - [x] Empty/whitespace input → returns `""` without throwing.

> **Deviatie van plan (bewust, documenteerbaar):** de plan-note "swap step 2 en 5" was onvoldoende. In de geïmplementeerde pipeline draaien `RemoveHidden()` en `ConvertInlineStylesToTags()` **vóór** `RemoveStyles()`, omdat beide de `style`-attribute lezen. Volgorde: scripts → comments → hidden → promote → styles → unwrap → classes/attrs → empty. Alle 10 tests (incl. hidden-removal en inline-style-promotion) passen, wat de volgorde bevestigt.

## Verification checklist
- [x] `dotnet test src/Imapster.HtmlViewer.Tests` — **Passed: 151, Failed: 0** (142 existing + 10 new converter tests); new-test-only filter: **Passed: 10, Failed: 0**.
- [x] `dotnet build src/Imapster/Imapster.csproj -c Debug` — **0 Warnings, 0 Errors** (main MAUI app compiles with the service change).
- [x] Self-review the diff: only the intended files touched — new `HtmlMarkdownConverter.cs`, new `HtmlMarkdownConverterTests.cs`, `EmailAiService.cs` (using + body line), and this plan file. csproj left as Colin's uncommitted version (BOM + ReverseMarkdown), verified untouched.
- [x] Measured real reduction via throwaway console harness (deleted after use): a ~10.8 KB realistic email HTML (style block + inline styles + `data-*` + `display:none` preheader) → **10,799 HTML bytes → 5,786 markdown bytes = 46.4% reduction**, output clean and readable.
- [ ] (Optional, manual) run the app, open an email, trigger classification, and confirm the `HtmlToMarkdown: … bytes HTML -> … bytes markdown` line in the debug console.

## Result
Implemented and verified. `ClassifyEmailAsync` now sends compact Markdown instead of raw HTML, with a per-call `Debug.WriteLine` byte-count line. Objective met: ~46% input-size reduction on a representative email.

## Handoff notes
- The working tree already contains Colin's uncommitted csproj change (BOM + ReverseMarkdown package) — do not revert or reformat it.
- Do not add `ReverseMarkdown` to the main `Imapster.csproj`; it flows transitively via the HtmlViewer project reference, and the class lives in HtmlViewer.
- Follow AGENTS.md: explicit usings, PascalCase, XML docs on public APIs, no MVVMTK involved here.
- Commit message (when Colin asks for the commit): `Add Html to Markdown conversion for AI classification` (or similar conventional style), including the csproj change and this plan file.

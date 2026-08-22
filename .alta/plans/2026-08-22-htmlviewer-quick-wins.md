# HtmlViewer quick wins: correcte fonthoogte + nette tabellen

- Status: Approved
- Plan file: `.alta/plans/2026-08-22-htmlviewer-quick-wins.md`
- Created: 2026-08-22
- Task: Kleine, gerichte verbeteringen aan Imapster.HtmlViewer: fonthoogte/baseline via echte font-metrics, basis tabelrendering (th bold, cell padding, content height, borders), alt-tekst en hr, en optionele quick wins (text-transform, legacy `<font>`).
- Git: not ignored; commit dit plan pas met de bijbehorende implementatie — maar **volgens gebruiker voor nu NIET committen** (gebruiker bekijk eerst).

## Objective
- Maak e-mails leesbaarder ("enigszins kan lezen" is de lat):
  - Fonthoogte/baseline moet voor elk font-grootte kloppen (nu hardcoded `fontSize × 1.2` / baseline `fontSize`).
  - Tabellen moeten er fatsoenlijk uitzien: kopcellen bold, cellen met padding, borders tekenen op de goede box.
- Non-goals:
  - Geen browser nabouwen: geen `<style>`-blokken/externe CSS, geen flex/grid, geen colspan/rowspan, geen position:absolute, geen image loading.
  - Geen netwerk: er wordt nergens verbinding met internet gemaakt (geverifieerd, zie bewijs).
  - Geen javascript (bestaat al niet).
  - Debug statements staan buiten scope (expliciet niet belangrijk voor gebruiker).
  - Geen commit (gebruiker bekijkt eerst).

## Context and evidence
- Line-height/baseline is op 5+ plekken hardcoded:
  - `src/Imapster.HtmlViewer/Layout/LayoutEngine.cs` — `BreakTextIntoLines` (~L806-911), `LayoutInlineNode` (~L486), `CreateLineBox` (~L660, `Baseline = fontSize`), `LayoutInlineChildrenOnLines` (~L921 `lineHeight = parentNode.FontSize * 1.2`).
  - Gevolg: een regel met gemengde groottes (bijv. 24px span in 16px tekst) krijgt één gedeelde baseline; grote tekst "zweeft" of overlapst, kleine tekst kijkt te laag. Baseline ligt bij Arial 16 ca. 0.9×fontSize, niet 1.0×.
- Geen internetgebruik (gecontroleerd): geen `HttpClient`/`WebRequest`/`Uri`/`SKBitmap` in `src/Imapster.HtmlViewer`; `<img>` wordt geparsed maar nooit geladen/gerenderd; `<style>`/extern CSS wordt genegeerd.
- Tabelrendering vandaag:
  - `<th>` krijgt niet automatisch bold: `HtmlParser.ParseNode` switch (L85-94) behandelt alleen Bold/Italic/Underline/Center; `TableHeaderCell` komt alleen uit `MapElementType` (L407).
  - `td`/`th` krijgen geen standaard padding (browsers: 1px 8px; nu 0).
  - `LayoutEngine.LayoutBlockNode` (L293-377) zet `ContentHeight` nooit in; `HtmlViewer.DrawBorders` (L737-742) gebruikt `node.ContentHeight` → cell borders tekenen op hoogte 0.
  - `LayoutTableRow` (L409-445) verdeelt breedtes gelijkmatig over cellen (ok voor deze scope).
- `<img>` zonder alt is onzichtbaar en `<hr>` wordt nooit getekend (geen case in `HtmlViewer.RenderNode`).
- `HtmlStyle.TextTransform` wordt geparsed (`HtmlParser.ParseInlineStyles` L203-205) en meege-mergd, maar nergens toegepast.
- Legacy `<font size="n">` komt voor in oude e-mails; nu gemapt naar `HtmlElementType.Unknown` (tekst blijft staan, grootte wordt genegeerd).
- Kleine performance: `HtmlViewer.OnPaintSurface` (L272) en `MeasureOverride` (L193) parsen de HTML opnieuw bij elke breedteverandering; layout kan hergebruikt worden.
- Tests die mee veranderen: `src/Imapster.HtmlViewer.Tests/BrTagRenderingTests.cs` (L86-88 verwacht `>= 16*1.2`), `BaselineAndDecorationTests.cs` (baseline-gelijkheid blijft gelden, waarden verschuiven).

## Assumptions and open decisions
- A1: Line-height `= ascent + descent + 2` (px) en baseline `= ascent + 1` (px) via `SKFont.Metrics` van het daadwerkelijke typeface. Fallback op `fontSize * 1.2` / `fontSize` als metrics afwijken (ascent ≤ 0).
- A2: Tabel-cel padding default 4px (kiesbaar: 1px 8px is browserstandaard; 4px is veiliger voor smalle layouts).
- A3: `text-transform` toepassen tijdens parsing op text nodes (uppercase/lowercase/capitalize/none); niet in layout/render.
- O1 (opgelost, gebruiker 2026-08-22 "Alles"): legacy `<font size>` WEL meenemen.
- O2 (opgelost, gebruiker 2026-08-22 "Alles"): `<td colspan="n">` breedteverdeling WEL meenemen.
- O3 (opgelost, gebruiker 2026-08-22 "Alles"): `text-transform` WEL in.
- Commit-instructie (gebruiker): NIET committen; gebruiker bekijkt de changes eerst. Planfile dus ook niet committen.

## Design notes
- Font-metrics helper (nieuwe private static in `LayoutEngine`, intern herbruikbaar):
  - `GetLineMetrics(LayoutNode node) -> (double Ascent, double Descent, double Baseline, double LineHeight)` via `SKFont(SKTypeface.FromFamilyName(node.FontFamily, weight, normal, slant)).Metrics` met `Size = node.FontSize`; fallback `ascent = fontSize*0.8`, `descent = fontSize*0.2`, `lineHeight = fontSize*1.2`.
  - Line-height per regel = max over alle spans op die regel (gemengde groottes), baseline per span = `lineHeight - descent(span) - 1 - lineGap/2 - ascent(span)` (baseline-align), waar `lineGap = lineHeight - maxAscent - maxDescent - 2`. Voor eén-grootte regels reduceert dit tot de eigen baseline (≈ oude gedrag, correcte waarde).
- Tabellen blijven simpel: gelijkmatige kolomverdeling, blokgewijze nesting (table → tbody → tr → td) werkt al; alleen styling/boxmodel aanvullen.
- Alternatieven verworpen:
  - `line-height` CSS-parsen: e-mails geven vaak `line-height:1.5em` mee; nu negeren (scope), metrics-vastzetten is de grootste win voor consistente hoogte.
  - Volledige inline flow herschrijven: te groot voor deze pass; alleen de bestaande lijnbox-paden corrigeren.
- Compatibiliteit: geen API- of XAML-veranderingen; `EmailDetailsView.xaml` blijft ongewijzigd.

## Risks and challenges
- R1: Baseline/line-height verschuift licht voor standaard tekst → enkele test-asserties moeten mee (`BrTagRenderingTests` L86, eventueel `BaselineAndDecorationTests`). Residu: visuele check via Demo-project of Imapster zelf.
- R2: `SKFont.Metrics` kan per platform iets anders zijn (Windows is primair; ok).
- R3: Selectie-rect in `RenderLine` (HtmlViewer L654-662) gebruikt `node.FontSize`-maten; blijft acceptabel, maar selectierect klopt dan minder exact bij gemengde groottes (bestaand gedrag).
- R4: `<font size>` + em/px mix: legacy mapping vermenigvuldigt parent-size; raak `MergeWith` (L163, `FontSize != 16`-check) niet aan — bestaand subtiel gedrag, buiten scope.

## Implementation checklist
- [ ] 1. `Layout/LayoutEngine.cs`: voeg private static `GetLineMetrics(LayoutNode)` toe (fallback-inclusief); vervang alle hardcoded `fontSize * 1.2` en `Baseline = fontSize` door metrics-gebaseerde waarden:
  - `BreakTextIntoLines` (line-height per segment, baseline per line box)
  - `CreateLineBox` (line-height + baseline parameters)
  - `LayoutInlineNode` en `LayoutInlineChildrenOnLines` (per-regel line-height = max over spans; child baseline offset)
- [ ] 2. `Rendering/HtmlViewer.cs` `RenderLine`: gebruik per-span baseline-offset (nieuw veld `BaselineOffset` op `LineBox.InlineStyleSpan`, gevuld door layout) in plaats van één gedeelde `line.Baseline`; decoraties (underline/line-through) per span op basis van die span-baseline.
- [ ] 3. `Layout/LineBox.cs`: voeg `double BaselineOffset` toe aan `InlineStyleSpan` (default 0 = shared baseline).
- [ ] 4. `Layout/LayoutEngine.cs` `LayoutBlockNode`: stel `node.ContentHeight` in (`totalHeight` minus padding/borders).
- [ ] 5. `Parsing/HtmlParser.cs`: `TableHeaderCell` krijgt `FontWeightBold = true` (switch in `ParseNode`, L85-94).
- [ ] 6. `Layout/LayoutEngine.cs` `ApplyDefaultMargins`: voeg `TableCell`/`TableHeaderCell` case toe met default padding 4px (alleen als niet expliciet gezet).
- [ ] 7. `Rendering/HtmlViewer.cs` `RenderNode`:
  - `HorizontalRule`: teken 1px lijn in `TextColor` (of #CCCCCC) met 1px verticale marge;
  - `Image`: als `Alt` niet leeg, teken `"[alt]"` inline in grijs (12px), op de content-baseline van de node.
- [ ] 8. `Rendering/HtmlViewer.cs`: kleine helper die 4-kantige borders tekent als `BorderTopWidth` enz. ingesteld zijn (rodeem bestaande `DrawBorders`; gebruik ContentHeight uit stap 4).
- [ ] 9. `Rendering/HtmlViewer.cs` `OnPaintSurface`/`MeasureOverride`: cache `_htmlRoot` — herparseer alleen als `HtmlContent` veranderd is (string-vergelijking); layout alsnog per breedte.
- [ ] 10. (O3 = ja) `Parsing/HtmlParser.cs`: pas `TextTransform` toe op text nodes in de subboom na het parsen (uppercase/lowercase/capitalize/none), met behoud van `&nbsp;` en structuur.
- [ ] 11. (O1 = ja) Legacy `<font>`: in `MapElementType` `font` → nieuw `HtmlElementType.Font` (Inline); parse attributen `size` (1-7) en `color` in `ParseNode`; in `LayoutEngine.ConvertHtmlNode` map `size` naar factor van parent `FontSize` (12/14/16/18/24/32/48 bij parent 16) via nieuw `HtmlStyle.FontSizeLegacyLevel` (0 = geen override).
- [ ] 12. (O2 = ja) `Layout/LayoutEngine.cs` `LayoutTableRow`: respecteer `colspan` uit `Attributes` (som van gelijke deeltjes).
- [ ] 13. Tests bijwerken: `BrTagRenderingTests.cs` L86-88 (verwachting 16*1.2 → tolerantie op metrics, bijv. `>= 18`); `BaselineAndDecorationTests.cs` controleren/aanpassen.
- [ ] 14. Nieuwe tests in `src/Imapster.HtmlViewer.Tests/` (xunit, bestaande stijl):
  - `MixedFontSizeLine_HasLargerLineHeight`: regel met 24px span in 16px paragraaf heeft grotere hoogte dan een 16px regel; baseline offsets verschillen.
  - `TableCell_DefaultPaddingApplied` + `TableHeaderCell_IsBold`.
  - `ContentHeight_IsSetForBlockNodes`.
  - `ImgWithAlt_LayoutKeepsAltText` (alt present in text stream) en `HorizontalRule_LayoutHasHeight`.
  - (O3 = ja) `TextTransform_UppercaseAppliedToTextNodes`.
  - (O1 = ja) `FontTag_SizeAppliedRelative`.
- [ ] 15. Build + test draaien en fouten oplossen (zie verificatie).

## Verification checklist
- [ ] `dotnet build Imapster.slnx` — geen warnings/fouten.
- [ ] `dotnet test src/Imapster.HtmlViewer.Tests -c Release` — alle tests groen, inclusief nieuwe.
- [ ] Handmatig (optioneel, Windows): `dotnet run` in `src/Imapster.HtmlViewer.Demo` — FBTO-demo-e-mail: regels staan niet te strak/overlappend, `Hartelijke groet,`/`FBTO` blijven op aparte regels.
- [ ] Handmatig: e-mail met tabel (2 rijen, th + td, border-left op cellen) — kop bold, padding zichtbaar, borders op de juiste hoogte.
- [ ] Zelfreview: `git diff` controleren op onbedoelde wijzigingen buiten `src/Imapster.HtmlViewer*`.

## Handoff notes
- Scope is bewust klein; aanraak `LayoutEngine` alleen op de genoemde plekken — de inline/linebox-logica is verstrengeld (`BuildParentLineStyleSpans` etc. moet blijven werken; tests `FbtoEmailBugTest`/`FbtoHtmlDebugTests` bewaken dat).
- Geen commit uitvoeren; gebruiker wil zelf eerst de changes bekijken. Plan-file zelf ook niet committen in deze pass.
- `EmailViewModel.BodyHtml` (`src/Imapster/ViewModels/EmailViewModel.cs` L66) wrappt plain text in `<pre>` — pre-pad blijft anders werken via metrics (line-height verandert); check visueel.
- Debug statements (Debug.WriteLine) staan expliciet buiten scope; laat ze staan.

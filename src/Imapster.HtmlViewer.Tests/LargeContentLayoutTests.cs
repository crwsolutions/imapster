using Imapster.HtmlViewer.Layout;
using Imapster.HtmlViewer.Parsing;
using Xunit;

namespace Imapster.HtmlViewer.Tests;

public class LargeContentLayoutTests
{
    private readonly LayoutEngine _layoutEngine;
    private readonly HtmlParser _htmlParser;

    public LargeContentLayoutTests()
    {
        _layoutEngine = new LayoutEngine();
        _htmlParser = new HtmlParser();
    }

    [Fact]
    public void Layout_LargeArticle_HasReasonableHeight()
    {
        // Arrange
        var html = """
                    <html>
                        <head>
                            <base href="https://www.parool.nl/nederland/camping-bakkum-de-meest-amsterdamse-ter-wereld-opent-weer-na-winterslaap-ik-kom-er-al-23-jaar-ben-verliefd-op-deze-plek~b73b81b9/"/>
                            <meta http-equiv="content-type" content="text/html; charset=utf-8"/>
                        </head>
                        <body style="overflow-wrap: break-word; -webkit-nbsp-mode: space; line-break: after-white-space;">
                            <div class="Apple-Mail-URLShareUserContentTopClass">
                                <br>
                            </div>
                            <div class="Apple-Mail-URLShareWrapperClass" style="position: relative !important;">
                                <blockquote type="cite" style="border-left-style: none; color: inherit; padding: inherit; margin: inherit;">
                                    <base href="https://www.parool.nl/nederland/camping-bakkum-de-meest-amsterdamse-ter-wereld-opent-weer-na-winterslaap-ik-kom-er-al-23-jaar-ben-verliefd-op-deze-plek~b73b81b9/"/>
                                    <style id="print">
                                        @media print {
                                            body {
                                                margin: 2mm 9mm;
                                            }

                                            .original-url {
                                                display: none;
                                            }

                                            #article .float.left {
                                                float: left !important;
                                            }

                                            #article .float.right {
                                                float: right !important;
                                            }

                                            #article .float {
                                                margin-top: 0 !important;
                                                margin-bottom: 0 !important;
                                            }
                                        }
                                    </style>
                                    <title>Camping Bakkum, de meest Amsterdamse ter wereld, opent weer na winterslaap: ‘Ik kom er al 23 jaar, ben verliefd op deze plek’ | Het Parool</title>
                                    <div>
                                        <div class="original-url">
                                            <br>
                                            <a href="https://www.parool.nl/nederland/camping-bakkum-de-meest-amsterdamse-ter-wereld-opent-weer-na-winterslaap-ik-kom-er-al-23-jaar-ben-verliefd-op-deze-plek~b73b81b9/">
                                                https://www.parool.nl/nederland/camping-bakkum-de-meest-amsterdamse-ter-wereld-opent-weer-na-winterslaap-ik-kom-er-al-23-jaar-ben-verliefd-op-deze-plek~b73b81b9/
                                            </a>
                                            <br>
                                            <br>
                                        </div>
                                        <div id="article" role="article" class="system exported" style="text-rendering: optimizelegibility; font-family: -apple-system-font; font-size: 1.2em; line-height: 1.5em; margin: 0px; padding: 0px;">
                                            <!-- This node will contain a number of div.page. -->
                                            <div class="page" style="text-align: start; overflow-wrap: break-word; max-width: 100%;">
                                                <h1 class="title" style="font-weight: bold; font-size: 1.95552em; line-height: 1.2141em; margin-top: 0px; margin-bottom: 0.5em; text-align: start; hyphens: manual; display: block; max-width: 100%;">
                                                    Camping Bakkum, de meest Amsterdamse ter wereld, opent weer na winterslaap: ‘Ik kom er al 23 jaar, ben verliefd op deze plek’
                                                </h1>
                                                <div class="metadata singleline" style="text-align: start; hyphens: manual; display: block; margin-bottom: 1.45em; margin-top: -0.75em; max-width: 100%;">
                                                    <a aria-label="auteur Marc Kruyswijk" rel="author" href="https://www.parool.nl/auteur/marc-kruyswijk/" class="byline" style="margin: 0px; color: rgb(65, 110, 210); max-width: 100%; text-decoration: underline; font-size: 1em !important; font-weight: normal !important; font-style: normal !important; display: inline !important;">Marc Kruyswijk</a>
                                                    <span class="delimiter" style="margin: 0.07em 0.45em 0px; max-width: 100%; padding: 0px; font-size: 1em !important; font-weight: normal !important; font-style: normal !important; display: inline !important;"></span>
                                                    <time datetime="2026-03-22T15:59:00.000Z" class="date" style="margin: 0px; max-width: 100%; font-size: 1em !important; font-weight: normal !important; font-style: normal !important; display: inline !important;">22 maart 2026, 16:59</time>
                                                </div>
                                                <header style="max-width: 100%;">
                                                    <figure style="max-width: 100%; font-size: 0.75em; line-height: 1.5em; font-family: -apple-system-font; color: rgba(0, 0, 0, 0.65); margin: 0px;">
                                                        <img alt="bij-camping-bakkum-moet-alles-zomerklaar-worden-gemaakt-maar.jpeg" height="6123" decoding="async" data-nimg="1" sizes="(max-width: 960px) 100vw, 1240px" srcset="https://image.parool.nl/271541929/width/640/bij-camping-bakkum-moet-alles-zomerklaar-worden-gemaakt-maar 640w, https://image.parool.nl/271541929/width/750/bij-camping-bakkum-moet-alles-zomerklaar-worden-gemaakt-maar 750w, https://image.parool.nl/271541929/width/828/bij-camping-bakkum-moet-alles-zomerklaar-worden-gemaakt-maar 828w, https://image.parool.nl/271541929/width/1080/bij-camping-bakkum-moet-alles-zomerklaar-worden-gemaakt-maar 1080w, https://image.parool.nl/271541929/width/1280/bij-camping-bakkum-moet-alles-zomerklaar-worden-gemaakt-maar 1280w, https://image.parool.nl/271541929/width/1920/bij-camping-bakkum-moet-alles-zomerklaar-worden-gemaakt-maar 1920w, https://image.parool.nl/271541929/width/2480/bij-camping-bakkum-moet-alles-zomerklaar-worden-gemaakt-maar 2480w" data-ats-category="image" style="max-width: 100%; margin: 0.5em auto; display: block; height: auto;" src="cid:240C8677-FC57-4D67-874F-5AC5F6E96CE2"/>
                                                        <figcaption style="max-width: 100%; margin-top: 0.8em; width: 100%; font-size: 0.75rem; color: rgba(0, 0, 0, 0.8);">
                                                            <span style="max-width: 100%; margin-top: 0.25em; margin-bottom: 0.25em;">Bij camping Bakkum moet alles zomerklaar worden gemaakt – maar niet zonder koffie en appeltaart.</span>
                                                            <cite style="max-width: 100%; margin-top: 0.25em; margin-bottom: 0.25em;">foto Harmen de Jong</cite>
                                                            <span style="max-width: 100%; margin-top: 0.25em; margin-bottom: 0.25em;"></span>
                                                        </figcaption>
                                                    </figure>
                                                    <p data-test-id="header-intro" data-ats-category="paragraph" style="max-width: 100%;">
                                                        Als Bakkum opent, is de winter voorbij en kan de zomer beginnen, zeggen ze op de meest Amsterdamse camping ter wereld. Dit weekend keerden de caravans terug op hun plek. Een logistieke monsterklus, maar vooral het begin van betere tijden.
                                                    </p>
                                                    <div style="max-width: 100%;">
                                                        <div style="max-width: 100%;">
                                                            <p style="max-width: 100%;">
                                                                is verslaggever verkeer en wonen van Het Parool
                                                            </p>
                                                        </div>
                                                    </div>
                                                </header>
                                                <section style="max-width: 100%;">
                                                    <div style="max-width: 100%;">
                                                        <div data-article-element-index="0" data-gtm-vis-first-on-screen101907915_1577="26736" data-gtm-vis-total-visible-time101907915_1577="500" data-gtm-vis-has-fired101907915_1577="1" style="max-width: 100%;">
                                                            <p data-ats-category="paragraph" style="max-width: 100%;">
                                                                Zoals dat gaat op een mooie Bakkumse lentedag: als je maar in de luwte zit en een beetje door je wimpers kijkt, kun je jezelf stiekem wijsmaken dat het zomer is. Dat de tijd van moeten voorbij is en die van mogen is aangebroken. Gewoon een beetje zitten voor je caravan. Een kopje koffie erbij. Je hand opsteken als de buurman langs drentelt.
                                                            </p>
                                                            <aside data-test-id="temptation-ARTICLE_INLINE_MIDDLE" data-temptation-position="ARTICLE_INLINE_MIDDLE" style="max-width: 100%;">
                                                                <aside data-module="temptation-module" data-nosnippet="true" style="max-width: 100%;">
                                                                    <div data-pexi-template="DEF_ENGAGE_BROWSER_PUSH_CHECK_ARTICLE_INLINE_MIDDLE_HP" data-ats-category="temptationPosition" style="max-width: 100%;">
                                                                        <p style="max-width: 100%;">
                                                                            Krijg een melding bij belangrijk nieuws over <span style="max-width: 100%;">Algemeen</span>.
                                                                        </p>
                                                                    </div>
                                                                </aside>
                                                            </aside>
                                                        </div>
                                                        <div data-article-element-index="1" data-gtm-vis-first-on-screen101907915_1577="28501" data-gtm-vis-total-visible-time101907915_1577="500" data-gtm-vis-has-fired101907915_1577="1" style="max-width: 100%;">
                                                            <p data-ats-category="paragraph" style="max-width: 100%;">
                                                                Her en der zie je ze inderdaad zo zitten. De zon die hun gezichten verwarmt, na maanden en maanden van kou, van regen, van sneeuw zelfs, nog niet eens zo gek lang geleden. Maar nu is het halverwege maart en kan het eindelijk weer. De stad is verruild voor het Noordhollands Duinreservaat. Geen toeterende auto’s meer, maar kwinkelerende vogels en het geruis van een windje door de takken boven je hoofd.
                                                            </p>
                                                        </div>
                                                        <p style="max-width: 100%;"></p>
                                                        <h2 style="font-weight: bold; font-size: 1.43em; max-width: 100%;">
                                                            Gestolen momenten
                                                        </h2>
                                                        <p></p>
                                                        <div data-article-element-index="3" data-gtm-vis-first-on-screen101907915_1577="29450" data-gtm-vis-total-visible-time101907915_1577="500" data-gtm-vis-has-fired101907915_1577="1" style="max-width: 100%;">
                                                            <p data-ats-category="paragraph" style="max-width: 100%;">
                                                                Je ziet ze zitten, genietend van de gestolen momenten. Want eigenlijk hebben ze helemaal geen tijd om te lanterfanten. Er is werk aan de winkel. Camping Bakkum, die meest Amsterdamse camping ter wereld, is weer open. Maar in Bakkum betekent dat dat de boel eerst zomerklaar moet worden gemaakt.
                                                            </p>
                                                        </div>
                                                        <div data-article-element-index="4" data-gtm-vis-first-on-screen101907915_1577="30553" data-gtm-vis-total-visible-time101907915_1577="500" data-gtm-vis-has-fired101907915_1577="1" style="max-width: 100%;">
                                                            <p data-ats-category="paragraph" style="max-width: 100%;">
                                                                Om te beginnen moeten de caravans terug op hun plekkie. Af en aan rijdt het over het uitgestrekte kampeerterrein: trekkers die nauwelijks mobiele huisjes achter zich aanslepen. Drie weekenden nemen ze daarvoor in Bakkum, om die kleine 1400 caravans vanaf de boer of het parkeerterrein aan de zijkant weer tussen de bomen te krijgen.
                                                            </p>
                                                        </div>
                                                        
                                                        <p style="max-width: 100%;"></p>
                                                        <h2 style="font-weight: bold; font-size: 1.43em; max-width: 100%;">Voor de 23ste keer</h2>
                                                        <p></p>
                                                        <div data-article-element-index="7" style="max-width: 100%;">
                                                            <p data-ats-category="paragraph" style="max-width: 100%;">
                                                                Schitterend dat ie er weer staat, zegt Laurie-Rose Born. “Eindelijk weer waar ie hoort, op plek 419. Het begint weer, we mogen er weer op, ik heb me hier zo op verheugd. Ik ben verliefd op Bakkum. Ik ga het 23ste jaar in. Ik heb wel eens gedacht: is het niet genoeg, mijn kinderen wilden niet meer mee. Maar kijk, Charlie is erbij, ze heeft me enorm geholpen.”
                                                            </p>
                                                        </div>
                                                        <div data-article-element-index="8" style="max-width: 100%;">
                                                            <p data-ats-category="paragraph" style="max-width: 100%;">
                                                                Charlie van 16 staat erbij en kijkt ernaar. Er waren inderdaad wat jaren waarin ze, zeg maar, niet zoveel zin had. “Ik hoop dat er dit jaar wel weer wat mensen komen van mijn leeftijd.”
                                                            </p>
                                                        </div>
                                                        <div data-article-element-index="9" style="max-width: 100%;">
                                                            <p data-ats-category="paragraph" style="max-width: 100%;">
                                                                Dat hoopt Laurie-Rose ook voor haar. Hoewel ze hier voor haar rust komt. “Straks staat mijn schutting, dan heb ik privacy. Ik kom dan ’s morgens de caravan uit en dan ga ik in een stoel zitten, een beetje luisteren naar de vogeltjes. Helemaal niks. Kom daar eens om in Amsterdam.”
                                                            </p>
                                                        </div>
                                                        <p style="max-width: 100%;"></p>
                                                        <h2 style="font-weight: bold; font-size: 1.43em; max-width: 100%;">De tuinstoelen staan al klaar</h2>
                                                        <p></p>
                                                        <div data-article-element-index="11" style="max-width: 100%;">
                                                            <p data-ats-category="paragraph" style="max-width: 100%;">
                                                                Even verderop is Wilma van der Heijden-Jonker al een eind verder met haar plek. De stoelen in een cirkel, voor je weet niet wie er aan komt waaien deze dag. Naast haar caravan staat al een tentje voor haar kleinzoon, die hier ook al zo graag komt. “Ik zou je graag een kopje koffie aanbieden, maar ik ben de sleutel van de caravan thuis vergeten, dus mijn dochters rijden even op en neer.”
                                                            </p>
                                                        </div>
                                                        <div data-article-element-index="12" style="max-width: 100%;">
                                                            <p data-ats-category="paragraph" style="max-width: 100%;">
                                                                Ook zij was er hard aan toe dat Bakkum weer openging, zegt ze. Want buiten, in de echte wereld, gaat het allemaal zo snel, daar raast het leven aan je voorbij. Ze heeft een druk sociaal leven, ze zingt in haar koor, niks te klagen, dank u zeer. Maar op Bakkum geniet ze van de kleine dingen. “Nu met die oorlogen, dat gedoe in de wereld, het maakt ook dat ik hier op de camping helemaal blij kan zijn, lekker aanrommelen. Leven mét mensen in plaats van langs elkaar heen.”
                                                            </p>
                                                        </div>
                                                        <p style="max-width: 100%;"></p>
                                                        <h2 style="font-weight: bold; font-size: 1.43em; max-width: 100%;">Niet chic, wel prettig</h2>
                                                        <p></p>
                                                        <div data-article-element-index="15" style="max-width: 100%;">
                                                            <p data-ats-category="paragraph" style="max-width: 100%;">
                                                                Geef ze eens ongelijk, op deze fantastische lentedag in maart. Het leven is mooi en niets op Bakkum doet daar iets aan af. Het leven speelt zich buiten af, zegt Annemarie Wuthrich, die zich opmaakt voor haar vijfde seizoen Bakkum. “Het is echt een natuurcamping. Niets chic, maar wel heel, heel prettig. De kinderen, ze zijn 2, 4 en 6 jaar oud, kunnen bijna zelfstandig buiten spelen. Je hoeft ze niet de hele tijd in de gaten te houden, zoals thuis in Amstelveen.”
                                                            </p>
                                                        </div>
                                                        <div data-article-element-index="16" style="max-width: 100%;">
                                                            <p data-ats-category="paragraph" style="max-width: 100%;">
                                                                Annemarie en haar man, bijgestaan door haar moeder, zijn nog druk met het overhevelen van inventaris van de oude caravan naar de nieuwe. “Dat het nu weer begint, dat we weer welkom zijn, voelt als het begin, en als het einde aan thuiszitten. Je hebt weer wat om naar uit te zien. We gaan hier best vaak heen, van vrijdagmiddag tot zondagavond. Als we hier zijn lijkt het weekend gewoon wat langer te duren.”
                                                            </p>
                                                        </div>
                                                        <p style="max-width: 100%;"></p>
                                                        <h2 style="font-weight: bold; font-size: 1.43em; max-width: 100%;">Altijd vijf minuten voor vijf</h2>
                                                        <p></p>
                                                        <div data-article-element-index="18" style="max-width: 100%;">
                                                            <p data-ats-category="paragraph" style="max-width: 100%;">
                                                                Je voelt het, het optimisme en de blijdschap, het zindert in de lucht. Wilma wijst ze aan, de caravans van haar buurtjes. En ze wijst op de klok die ze net heeft opgehangen aan de boom naast haar zomerverblijf. “Die staat altijd op hetzelfde moment, vijf minuten voor vijf. Want als de vijf in de klok zit, dan kunnen we weer gaan borrelen.” Maar nu eerst koffie en appeltaart.
                                                            </p>
                                                        </div>
                                                        <div data-article-element-index="19" style="max-width: 100%;">
                                                            <p data-ats-category="paragraph" style="max-width: 100%;">
                                                                Laurie-Rose, geboren en getogen Jordanese, legt dochter Charlie uit hoe ze vroeger, in haar jonge jaren, het nog wel eens te bont maakte op Bakkum. Dan heb je Charlies aandacht wel. “Ik werd er bijna uit gegooid, zo gezellig waren de feestjes die ik hier had. Maar dat was toen, nu kom ik hier om te ontspannen. Heerlijk dat het weer begint.” Charlie kijkt bedenkelijk, nét een beetje meer actie in de tent zou zij dan weer niet erg vinden.
                                                            </p>
                                                        </div>
                                                        <div data-article-element-index="20" style="max-width: 100%;">
                                                            <p data-ats-category="paragraph" style="max-width: 100%;">
                                                                <b style="max-width: 100%;">Over de auteur: <a href="https://www.parool.nl/auteur/marc-kruyswijk/" target="_blank" style="color: rgb(65, 110, 210); max-width: 100%; text-decoration: underline;">Marc Kruyswijk</a> schrijft al meer dan tien jaar als verslaggever voor Het Parool over wonen in Amsterdam en verkeer &amp; vervoer in de stad.</b>
                                                            </p>
                                                        </div>
                                                    </div>
                                                    <p style="max-width: 100%;">
                                                        <span style="max-width: 100%;">Help ons door uw ervaring te delen: </span>
                                                    </p>
                                                    <div style="max-width: 100%;">
                                                        <section data-test-id="editorial-tips" style="max-width: 100%;">
                                                            <h2 style="font-style: italic; font-weight: bold; font-size: 1.43em; max-width: 100%;">
                                                                Lees ook
                                                            </h2>
                                                            <p style="max-width: 100%;">
                                                                Geselecteerd door de redactie
                                                            </p>
                                                        </section>
                                                    </div>
                                                </section>
                                            </div>
                                        </div>
                                    </div>
                                </blockquote>
                            </div>
                        </body>
                    </html>
        """;

        // Act
        var htmlRoot = _htmlParser.Parse(html);
        var layoutRoot = _layoutEngine.Layout(htmlRoot, 800);

        // Assert - Verify that the layout height is less than 10000 as requested
        Assert.NotNull(layoutRoot);
        Assert.True(layoutRoot.Height > 0, "Layout should have a positive height");
        Assert.True(layoutRoot.Height < 10000, $"Layout height {layoutRoot.Height} should be less than 10000");
    }
}
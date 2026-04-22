using CommunityToolkit.Mvvm.ComponentModel;

namespace Imapster.HtmlViewer.Demo.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _html = @"
            <body>
Beste meneer Jansen,<br /><br />Wij hebben declaraties voor u verwerkt.<br /><br /><strong><strong style=""color:#005eaa; font-weight:bold"">Op uw declaratieoverzicht ziet u welke declaraties wij voor u verwerkten</strong> </strong><br />Ook ziet u meteen de stand van uw eigen risico. Het overzicht staat klaar op Zorggebruik onder uw <a title=""Berichtenbox"" target=""_blank"" href=""https://mijnzorg.fbto.nl/"">Berichtenbox</a>. Inloggen doet u veilig &eacute;n snel met DigiD en SMS-controle of met de DigiD-app.<br /><br /><strong><strong style=""color:#005eaa; font-weight:bold"">Kunnen we nog iets voor u doen?</strong> </strong><br />Heeft u vragen? Kijk dan op <a href=""https://www.fbto.nl/zorgverzekering"">fbto.nl/zorg</a>. Of neem contact met ons op. Op <a href=""https://www.fbto.nl/verzekeringen/contact"">fbto.nl/contact</a> leest u hoe u ons bereikt.<br /><br />Hartelijke groet,<br /><br />FBTO<br/><br/>
</body>
        ";
}

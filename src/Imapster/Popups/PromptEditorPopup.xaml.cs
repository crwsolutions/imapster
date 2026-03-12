namespace Imapster.Popups;

public partial class PromptEditorPopup : Popup<string>
{
    public PromptEditorPopup()
    {
        InitializeComponent();
        this.Loaded += async (s, e) => await ((PromptEditorPopupViewModel)BindingContext!).LoadPromptCommand.Execute(null);
    }
}
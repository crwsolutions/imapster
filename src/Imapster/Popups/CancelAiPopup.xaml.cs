namespace Imapster.Popups;

public partial class CancelAiPopup
{
    private readonly CancellationTokenSource _cts;

    public CancelAiPopup(CancellationTokenSource cts)
    {
        _cts = cts;
        InitializeComponent();
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        _cts.Cancel();

    }
}
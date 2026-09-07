namespace Imapster.ViewModels;

/// <summary>
/// Backing ViewModel for the fire-and-forget AI classification progress popup.
/// The popup is shown without awaiting a result; the ViewModel cancels the
/// running work when the user taps Cancel. The owning ViewModel closes the
/// popup once the work finishes or is interrupted.
/// </summary>
public partial class CancelAiViewModel : ObservableObject, IQueryAttributable
{
    private CancellationTokenSource? _cts;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("cts", out var ctsObj) && ctsObj is CancellationTokenSource cts)
        {
            _cts = cts;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
    }
}

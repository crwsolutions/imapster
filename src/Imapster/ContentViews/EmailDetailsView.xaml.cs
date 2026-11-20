using System.Diagnostics;
using Imapster.Diagnostics;

namespace Imapster.ContentViews;

public partial class EmailDetailsView : ContentView
{
    public static readonly BindableProperty EmailProperty =
        BindableProperty.Create(nameof(Email), typeof(EmailViewModel), typeof(EmailDetailsView),
            null, BindingMode.TwoWay, propertyChanged: OnEmailChanged);

    public EmailViewModel? Email
    {
        get => (EmailViewModel?)GetValue(EmailProperty);
        set => SetValue(EmailProperty, value);
    }

    private static void OnEmailChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is EmailDetailsView view)
        {
            view.UpdateVisibility();
        }

        if (newValue is EmailViewModel newEmail)
        {
            LoadTrace.Log($"open email: '{newEmail.Subject}' (id {newEmail.Id}, body {newEmail.Body?.Length:N0} chars)");
            var sw = Stopwatch.StartNew();
            newEmail.UpdateCanArchive();
            sw.Stop();
            LoadTrace.Log($"  UpdateCanArchive: {sw.ElapsedMilliseconds} ms");

            // The raw body view is populated on demand instead of bound; refresh it here
            // when it is the visible body view.
            if (bindable is EmailDetailsView rawView && rawView.RawContentGrid.IsVisible)
                rawView.RawContentGrid.Text = newEmail.Body ?? string.Empty;
        }
    }

    public EmailDetailsView()
    {
        InitializeComponent();

        // Initialize to Html tab (default)
        //ShowTab("html");
    }

    private void UpdateVisibility()
    {
        if (Email != null)
        {
            EmptyStateGrid.IsVisible = false;
            DetailsScrollView.IsVisible = true;
        }
        else
        {
            EmptyStateGrid.IsVisible = true;
            DetailsScrollView.IsVisible = false;
        }
    }

    private void ToggleSwitchView_Toggled(object? sender, bool isShowingRaw)
    {
            HtmlContentGrid.IsVisible = !isShowingRaw;
            RawContentGrid.IsVisible = isShowingRaw;

            // Populate the raw view on demand: the full HTML body can be 100+ KB, and
            // pushing it through the WinUI text control on every email open made large
            // mails take minutes to open. Clear it when hidden to keep the body out of
            // the text control.
            RawContentGrid.Text = isShowingRaw ? Email?.Body ?? string.Empty : string.Empty;
    }

    private void ToggleFromButtonClicked(object? sender, EventArgs e)
    {
        if (FromToDetailsGrid.IsVisible)
        {
            FromToDetailsGrid.IsVisible = false;
            FromPill.IsVisible = true;
            ToggleFromButton.Text = "Gegevens";
        }
        else
        {
            FromToDetailsGrid.IsVisible = true;
            FromPill.IsVisible = false;
            ToggleFromButton.Text = "Verbergen";
        }
    }
}

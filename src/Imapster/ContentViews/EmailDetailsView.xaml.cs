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
            newEmail.UpdateCanArchive();
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

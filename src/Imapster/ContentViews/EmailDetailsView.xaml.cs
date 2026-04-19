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
        ShowTab("html");
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

    private void OnHtmlTabClicked(object? sender, EventArgs e) => ShowTab("html");

    private void OnRawTabClicked(object? sender, EventArgs e) => ShowTab("raw");

    private void ShowTab(string tab)
    {
        var primaryColor = (Color)Application.Current!.Resources["Primary"];
        var grayColor = (Color)Application.Current!.Resources["Gray400"];

        if (tab == "html")
        {
            HtmlContentGrid.IsVisible = true;
            RawContentGrid.IsVisible = false;
            HtmlTabButton.BackgroundColor = primaryColor;
            RawTabButton.BackgroundColor = grayColor;
        }
        else
        {
            HtmlContentGrid.IsVisible = false;
            RawContentGrid.IsVisible = true;
            HtmlTabButton.BackgroundColor = grayColor;
            RawTabButton.BackgroundColor = primaryColor;
        }
    }
}

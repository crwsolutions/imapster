using Imapster.HtmlViewer.Demo.ViewModels;

namespace Imapster.HtmlViewer.Demo;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

using Imapster.Extensions;

namespace Imapster;

public partial class AppWindow : Window
{
    public AppWindow(AppShell appShell)
    {
        InitializeComponent();
        Page = appShell;

        var height = 1080D;
        var width = 1620D;

        var screen = GetScreenDimensions();
        Height = Math.Min(screen.Height, height);
        Width = Math.Min(screen.Width, width);
        X = (screen.Width - Width) / 2;
        Y = (screen.Height - Height) / 2;
    }

    private static (double Width, double Height) GetScreenDimensions()
    {
        var workSize = DeviceDisplay.Current.MainDisplayInfo.GetWorkArea();
        var density = DeviceDisplay.Current.MainDisplayInfo.Density;
        var width = workSize.Width / density;
        var height = workSize.Height / density;

        return (width, height);
    }
}

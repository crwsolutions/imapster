using Imapster.Services;

internal static class AddViewsExtension
{
    internal static MauiAppBuilder AddViews(this MauiAppBuilder builder)
    {

        // Register views and view models
        builder.Services.AddSingleton<MainPage, MainViewModel>();

        builder.Services.AddSingleton<EmailAiService>();

        return builder;
    }
}


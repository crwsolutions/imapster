using Imapster.Repositories;
using Imapster.Services;

internal static class AddViewsExtension
{
    internal static MauiAppBuilder AddViews(this MauiAppBuilder builder)
    {

        // Register views and view models
        builder.Services.AddTransient<MainPage, MainViewModel>();

        builder.Services.AddTransient<EmailAiService>();

        return builder;
    }
}


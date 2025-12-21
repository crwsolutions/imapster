using Imapster.Services;
using Imapster.Repositories;

internal static class AddViewsExtension
{
    internal static MauiAppBuilder AddViews(this MauiAppBuilder builder)
    {
        // Register repositories
        builder.Services.AddSingleton<IFolderRepository, FolderRepository>();
        builder.Services.AddSingleton<IEmailRepository, EmailRepository>();
        builder.Services.AddSingleton<IAccountRepository, AccountRepository>();
        
        // Register services
        builder.Services.AddSingleton<IImapSyncService, ImapSyncService>();
        
        // Register views and view models
        builder.Services.AddTransient<MainPage, MainViewModel>();
        
        builder.Services.AddTransient<EmailAiService>();

        return builder;
    }
}


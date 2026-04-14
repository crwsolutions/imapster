using CommunityToolkit.Maui.Storage;
using Imapster.Popups;
using Imapster.Repositories;
using Imapster.Services;
using Microsoft.Extensions.AI;
using OpenAI;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System.ClientModel;

namespace Imapster
{
    public static class MauiProgram
    {
        private static async Task CleanupTempDirectoryAsync()
        {
            try
            {
                var tempDir = Path.Combine(FileSystem.AppDataDirectory, "temp");
                if (Directory.Exists(tempDir))
                {
                    foreach (var file in Directory.GetFiles(tempDir))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                // Fire and forget - don't crash the app on cleanup failure
                Debug.WriteLine($"Failed to cleanup temp directory: {ex.Message}");
            }
        }

        public static MauiApp CreateMauiApp()
        {
            //var endpoint = "http://localhost:11434/";
            var endpoint = "http://localhost:8080/";
            var modelId = "hf.co/unsloth/Qwen3-Coder-30B-A3B-Instruct-GGUF:Q5_K_XL";

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("FontAwesome6FreeSolid.otf", "FontAwesomeSolid");
                })
                .AddViews()
                //.Services.AddChatClient(ChatClientBuilderChatClientExtensions.AsBuilder(new OllamaApiClient(endpoint, modelId))
                //    .UseFunctionInvocation()
                //    .Build());
                .Services.AddChatClient(new OpenAIClient(
                    new ApiKeyCredential("dummy"), // llama.cpp requires one, value ignored
                    new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
                    .GetChatClient(modelId)
                    .AsIChatClient()
                    .AsBuilder()
                    .UseFunctionInvocation()
                    .Build());

#if DEBUG
            builder.Logging.AddDebug();
#endif
            // Register repositories
            builder.Services.AddSingleton<IFolderRepository, FolderRepository>();
            builder.Services.AddSingleton<IEmailRepository, EmailRepository>();
            builder.Services.AddSingleton<IAccountRepository, AccountRepository>();
            builder.Services.AddSingleton<IPromptRepository, PromptRepository>();

            // Register services
            builder.Services.AddSingleton<IImapSyncService, ImapSyncService>();
            builder.Services.AddSingleton<IAttachmentService, AttachmentService>();

            builder.Services.AddSingleton(FolderPicker.Default);

            builder.Services.AddTransientPopup<PromptEditorPopup, PromptEditorPopupViewModel>();
            builder.Services.AddTransientPopup<MoveFolderPopup, MoveFolderPopupViewModel>();
            builder.Services.AddTransientPopup<EmailDetailsPopup, EmailDetailsViewModel>();

            var app = builder.Build();
            Database.Initialize();
            _ = CleanupTempDirectoryAsync();
            return app;
        }
    }
}

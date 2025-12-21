using Imapster.Repositories;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OpenAI;
using System.ClientModel;

namespace Imapster
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            //var endpoint = "http://localhost:11434/";
            var endpoint = "http://localhost:8080/";
            var modelId = "hf.co/unsloth/Qwen3-Coder-30B-A3B-Instruct-GGUF:Q5_K_XL";

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
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

            var app = builder.Build();
            Database.Initialize();
            return app;
        }
    }
}

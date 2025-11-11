using ArtistTool.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArtistTool.Intelligence
{
    public static class Services
    {
        public static IServiceCollection AddIntelligenceServices(this IServiceCollection services, Func<string, string> config)
        {
            services.AddSingleton<PhotoIntelligenceService>();
            // Register database with proper async initialization
            services.AddSingleton<IPhotoDatabase, IntelligentPhotoDatabase>();
            services.AddSingleton<IAIClientProvider>(sp => new AzureOpenAIClientProvider(
                config("AzureOpenAI:Endpoint")!,
                config("AzureOpenAI:ConversationalDeployment")!,
                config("AzureOpenAI:VisionDeployment")!,
                config("AzureOpenAI:ImageDeployment")!,
                sp.GetRequiredService<ILoggerFactory>()));

            return services;
        }
    }
}

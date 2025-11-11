using Microsoft.Extensions.DependencyInjection;

namespace ArtistTool.Services
{
    public static class Services
    {
        // General services can be registered here in the future
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddSingleton<IImageManager, ImageManager>();
            services.AddSingleton<IImageRegistry, ImageRegistry>();
            // Add any common services needed across the application
            return services;
        }
    }
}

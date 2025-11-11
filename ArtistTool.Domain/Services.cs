using Microsoft.Extensions.DependencyInjection;

namespace ArtistTool.Domain
{
    public static class Services
    {
        // Domain services can be registered here in the future
        public static IServiceCollection AddDomainServices(this IServiceCollection services)
        {
            services.AddSingleton<IPhotoDatabase, PersistentPhotoDatabase>();
            return services;
        }
    }
}

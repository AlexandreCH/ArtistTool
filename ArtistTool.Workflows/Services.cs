using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArtistTool.Workflows
{
    public static class Services
    {
        /// <summary>
        /// Registers all workflow-related services including client cache, parser, and executors.
        /// Note: After calling this, you must call InitializeClientsAsync() to load clients from the markdown file.
        /// </summary>
        /// <param name="services">The service collection to add services to</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddWorkflowServices(this IServiceCollection services)
        {
            // Register the client factory for creating actual client instances
            services.AddSingleton<ClientFactory>();
            services.AddSingleton<ResearchFactory>();
            services.AddSingleton<ReportService>();
            services.AddSingleton<MediumWorkflow>();
            services.AddSingleton<CritiqueWorkflow>();
            services.AddSingleton<MarketingWorkflow>();
            // Register the workflows host runner 
            services.AddSingleton<WorkflowsHost>();
            services.AddHostedService(sp => sp.GetRequiredService<WorkflowsHost>());
   
            return services;
        }
        
        /// <summary>
        /// Initializes AI clients from the Agents.md markdown file.
        /// This should be called during application startup after the service provider is built.
        /// </summary>
        /// <param name="serviceProvider">The service provider to resolve dependencies from</param>
        /// <param name="clientsMarkdownPath">Optional path to the Agents.md file. If null, defaults to "Agents.md" in the application directory.</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public static async Task InitializeClientsAsync(
            this IServiceProvider serviceProvider)
        {
            // start the workflows host to listen for workflow start requests
            var workflowsHost = serviceProvider.GetRequiredService<WorkflowsHost>();
            var result = workflowsHost.StartAsync(CancellationToken.None);
        }
    }
}

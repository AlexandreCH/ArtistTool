using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArtistTool.Workflows
{
    public static class Services
    {
        /// <summary>
        /// Registers all workflow-related services including agents, parsers, and executors.
        /// Note: After calling this, you must call InitializeAgentsAsync() to load agents from the markdown file.
        /// </summary>
        /// <param name="services">The service collection to add services to</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddWorkflowServices(this IServiceCollection services)
        {
            // Register the agent cache as a singleton to maintain agent instances
            services.AddSingleton<AgentCache>();
            
            // Register the markdown parser for agent configuration
            services.AddSingleton<AgentMarkdownParser>();
            
            // Register the workflows host runner 
            services.AddHostedService<WorkflowsHost>();
            services.AddSingleton(sp => sp.GetServices<IHostedService>().OfType<WorkflowsHost>().First());
            services.AddSingleton<IMarketingWorkflowController>(
                sp => sp.GetRequiredService<WorkflowsHost>());

            // Register workflow executors
            services.AddSingleton(sp => 
                new CritiqueExecutor(nameof(CritiqueExecutor), sp.GetRequiredService<AgentCache>()));
            services.AddSingleton(sp =>
                new GenerateReportExecutor(
                    nameof(GenerateReportExecutor),
                sp.GetRequiredService<ILogger<GenerateReportExecutor>>()));

            return services;
        }
        
        /// <summary>
        /// Initializes agents from the Agents.md markdown file.
        /// This should be called during application startup after the service provider is built.
        /// </summary>
        /// <param name="serviceProvider">The service provider to resolve dependencies from</param>
        /// <param name="agentsMarkdownPath">Optional path to the Agents.md file. If null, defaults to "Agents.md" in the application directory.</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public static Task InitializeAgentsAsync(
            this IServiceProvider serviceProvider, 
            string? agentsMarkdownPath = null)
        {
            var parser = serviceProvider.GetRequiredService<AgentMarkdownParser>();
            var markdownPath = agentsMarkdownPath ?? Path.Combine(AppContext.BaseDirectory, "Agents.md");
            
            if (File.Exists(markdownPath))
            {
                var markdown = File.ReadAllText(markdownPath);
                parser.Parse(markdown);
            }
            else
            {
                throw new FileNotFoundException(
                    $"Agents configuration file not found at: {markdownPath}");
            }

            // start the workflows host to listen for workflow start requests
            var workflowsHost = serviceProvider.GetRequiredService<WorkflowsHost>();
            workflowsHost.StartAsync(CancellationToken.None);

            return Task.CompletedTask;
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ArtistTool.Workflows
{
    public class WorkflowsHost(IServiceProvider sp) : IHostedService, IMarketingWorkflowController
    {
        private bool started;
        private readonly ILogger<WorkflowsHost> _logger =
            sp.GetRequiredService<ILogger<WorkflowsHost>>();
        
        private readonly ConcurrentDictionary<string, MarketingWorkflow> _workflows = [];

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (started)
            {
                return Task.CompletedTask;
            }
            started = true;
            _logger.LogInformation("WorkflowsHost starting.");
           
            return Task.CompletedTask;
        }

        public async Task<MarketingWorkflowContext> GetOrStartMarketingWorkflowAsync(string photoId)
        {
            if (_workflows.TryGetValue(photoId, out MarketingWorkflow? value) && value is not null)
            {
                return value.Context;
            }
            
            var workflow = new MarketingWorkflow(sp);

            if (await workflow.InitAsync(photoId) && _workflows.TryAdd(photoId, workflow))
            {
                _logger.LogInformation("Started workflow for photo {PhotoId}.", photoId);
                return workflow.Context;
            }
            else
            {
                throw new InvalidOperationException($"Failed to initialize workflow for {photoId}.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("WorkflowsHost stopping.");
            return Task.CompletedTask;
        }
    }
}

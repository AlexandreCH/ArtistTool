using ArtistTool.Services;
using ArtistTool.Workflows.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArtistTool.Workflows
{
    public class WorkflowsHost(
        IMessageHub messageHub,
        ILoggerFactory factory,
        ReportService reportService,
        MarketingWorkflow marketingWorkflow) : IHostedService
    {
        private bool started;
        private List<Task> workflows = [];
        private readonly ILogger _logger =
            factory.CreateLogger<WorkflowsHost>();
     
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (started)
            {
                return Task.CompletedTask;
            }
            started = true;
            _logger.LogInformation("WorkflowsHost starting.");
           
            var subscription = messageHub.Subscribe<WorkflowRequested<string>>(
                async message =>
                {
                    _logger.LogDebug("Received JobStartedFor message for job with name {JobName} and payload: {Payload}",
                        message.Name, message.Payload);
                    
                    if (message.Name == nameof(MarketingWorkflow))
                    {
                        var photoId = message.Payload;
                        _logger.LogDebug("Marketing Workflow requested for Photo ID: {PhotoId}", photoId);
                        workflows.Add(marketingWorkflow.StartWorkflowAsync(photoId));
                    }
                });

            var sub2 = messageHub.Subscribe<WorkflowRequested<MarketReport>>(async reportWrapper =>
            {
                var mr = reportWrapper.Payload;
                if (mr.AnalysisDone && mr.ReportWritingPct <= 0)
                {
                    _logger.LogDebug("Analysis for workflow is commplete. Requesting a report generation for {PhotoId}", mr.Id);
                    workflows.Add(reportService.PublishReportAsync(mr));
                }
            });     

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("WorkflowsHost stopping.");
            return Task.CompletedTask;
        }
    }
}

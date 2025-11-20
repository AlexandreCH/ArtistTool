
using ArtistTool.Domain;
using ArtistTool.Domain.Agents;
using ArtistTool.Services;
using ArtistTool.Workflows.Messages;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ArtistTool.Workflows
{
    public class MarketingWorkflow(
        IProjectManager pm,
        IMessageHub messageHub,
        IPhotoDatabase db,
        ILoggerFactory factory, 
        CritiqueWorkflow critique,
        MediumWorkflow medium)
    {
        private ConcurrentBag<string> photoIds = [];

        private readonly ILogger logger = factory.CreateLogger<MarketingWorkflow>();

        private static MediumReport AddOrGet(string medium, MarketReport report)
        {
            var existing = report.MediumReports.FirstOrDefault(r => r.Medium == medium);

            if (existing is not null)
            {
                return existing;
            }

            var newReport = new MediumReport(medium);
            report.MediumReports = [newReport, .. report.MediumReports];
            return newReport;
        }

        public async Task StartWorkflowAsync(string photoId)
        {
            try
            {
                logger.LogDebug("Starting Marketing Workflow for Photo: {PhotoId}", photoId);

                if (photoIds.Contains(photoId))
                {
                    logger.LogWarning("Workflow for photo {PhotoId} is already running. Skipping duplicate execution.", photoId);
                    return;
                }

                photoIds.Add(photoId);

                var photo = await db.GetPhotographWithIdAsync(photoId) ?? throw new InvalidOperationException($"Photo with id {photoId} not found.");

                var reportNumber = await pm.InitReportAsync(photo!.Id);
                var report = new MarketReport(photo, reportNumber);
                report.Status = "Building the analysis workflow...";
                messageHub.Publish(report);
                messageHub.Publish(new JobStartedFor<MarketReport>(photoId));
                messageHub.Publish(new ReportUpdate(photoId, report));

                var start = new ChatMessageExecutor((ctx, cm) => ValueTask.FromResult(cm), "Start");

                var workflowBuilder = new WorkflowBuilder(start);

                var critiqueExecutor = await critique.GetCritiqueExecutorAsync();

                workflowBuilder.BindExecutor(critiqueExecutor).WithOutputFrom(critiqueExecutor);

                var (mediumExecutors, researchExecutors) = await medium.GetMediumExecutorsAsync(workflowBuilder);

                ExecutorBinding[] bindings = [critiqueExecutor, .. mediumExecutors];

                workflowBuilder.AddFanOutEdge(start, [.. bindings]);
                   
                var aggregator = new AggregatingExecutor<ChatMessage, ChatMessage>(nameof(AggregatingExecutor<,>),
                    AggregateChat);

                workflowBuilder.BindExecutor(aggregator);

                workflowBuilder.AddFanInEdge([critiqueExecutor, .. researchExecutors], aggregator);

                var workflow = workflowBuilder.Build();

                report.Workflow = workflow.ToMermaidString();
                report.Status = "Workflow built. Starting execution...";
                messageHub.Publish(new ReportUpdate(photoId, report));

                logger.LogDebug("Workflow built successfully. Starting execution for Photo: {PhotoId}", photoId);

                var photoMetaContent = photo.AsSerializedChatParameter();
                photoMetaContent.Name = $"{nameof(Photograph)} meta";

                var photoContent = photo!.AsSerializedPhoto();
                photoContent.Name = "Photograph";

                var photoIdContent = new TextContent($"PhotoId: {photoId}");
                var reportId = await pm.InitReportAsync(photoId);

                var initialMessage = new ChatMessage(ChatRole.User, [photoIdContent, photoContent, photoMetaContent]);
               
                // Execute the workflow
                await using StreamingRun run = await InProcessExecution.StreamAsync(workflow,
                    initialMessage);

                messageHub.Publish(new JobStartedFor<MarketingWorkflow>(photoId));

                await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

                await foreach (WorkflowEvent evt in run.WatchStreamAsync())
                {
                    logger.LogDebug("Received workflow event of type: {EventType}", evt.GetType().Name);

                    if (evt is WorkflowOutputEvent outputEvent)
                    {
                        logger.LogInformation("Published output message for Photo: {PhotoId} with type {Type}", photoId, outputEvent.Data?.GetType());

                        bool changes = false;
                        switch (outputEvent.Data)
                        {
                            case CritiqueResponse cr:
                                report.Critique ??= cr;
                                report.Status = "Received image critique.";
                                changes = true;
                                break;

                            case MediumPreviewResponse mr:
                                var mediumEntry = AddOrGet(mr.Medium, report);
                                mediumEntry.MediumPreview ??= mr.Preview;
                                report.Status = $"Generated preview for {mediumEntry.Medium}";
                                changes = true;
                                break;

                            case ResearchResponse resp:
                                var medium = AddOrGet(resp.Medium, report);
                                medium.ResearchResponses = [resp, .. medium.ResearchResponses];
                                changes = true;
                                report.Status = $"Completed research for {medium.Medium}";
                                break;

                            case PriceResponse pr:
                                var med = AddOrGet(pr.Medium, report);
                                med.PriceResponse = pr;
                                report.Status = $"Completed price analysis for {med.Medium}";
                                changes = true;
                                break;

                            case ProductResearchResponse prod:
                                var medProd = AddOrGet(prod.Medium, report);
                                medProd.ProductResearchResponse = prod;
                                report.Status = $"Finished product research for {medProd.Medium}";
                                changes = true;
                                break;

                            case MarketingCampaignResponse mc:
                                var medCamp = AddOrGet(mc.Medium, report);
                                medCamp.MarketingCampaignResponse = mc;
                                report.Status = $"Created marketing campaign for {medCamp.Medium}";

                                changes = true;
                                break;

                            case SocialMediaChannelResponse sm:
                                var medSoc = AddOrGet(sm.Medium, report);
                                medSoc.Socials = [sm, .. medSoc.Socials];
                                changes = true;
                                report.Status = $"Created {sm.ChannelName} content strategy for {sm.Medium}";
                                break;

                            default:
                                logger.LogDebug("Didn't process message {Message}", outputEvent.Data);
                                break;
                        }

                        if (changes)
                        {
                            messageHub.Publish(new ReportUpdate(photoId, report));
                        }
                    }
                }

                
                logger.LogDebug("Workflow execution completed for {PhotoId}", photoId);

                report.Status = "Workflow execution completed.";
                report.AnalysisDone = true;
                messageHub.Publish(new ReportUpdate(report.Id, report));
                messageHub.Publish(new JobEndedFor<MarketingWorkflow>(photoId, this));
                messageHub.Publish(new WorkflowRequested<MarketReport>(photoId, report)); 
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred while building the marketing workflow.");
                throw;
            }
        }

        private ChatMessage AggregateChat(ChatMessage? message1, ChatMessage message2)
        {
            if (message1 is null)
            {
                return new ChatMessage(ChatRole.Assistant, [..message2.Contents]);
            }
            List<AIContent> merge = [..message1.Contents];
            merge.AddRange(message2.Contents);
            return new ChatMessage(ChatRole.Assistant, merge);
        }
    }
}
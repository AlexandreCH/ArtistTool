
using ArtistTool.Domain;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArtistTool.Workflows
{
    public class MarketingWorkflow(IServiceProvider sp)
    {
        private Timer? _timer;

        private Workflow? _workflow = null;

        private readonly MarketingWorkflowContext _context = new();

        private readonly ILogger<MarketingWorkflow> _logger =
            sp.GetRequiredService<ILogger<MarketingWorkflow>>();

        public MarketingWorkflowContext Context => _context;

        public async Task<bool> InitAsync(string id)
        {
            _logger.LogDebug($"Init called for {nameof(MarketingWorkflow)} {{photoId}}", id);

            if (_workflow is not null)
            {
                return false;
            }

            _context.Photo = await sp.GetRequiredService<IPhotoDatabase>().GetPhotographWithIdAsync(id);

            _timer = new Timer(async _ => await RunWorkflowAsync(null), null, 200, 999999);

            return true;
        }

        private async Task RunWorkflowAsync(object? state)
        {
            List<ExecutorBinding> fanIn = [];

            _timer?.Dispose();
            _timer = null;

            var agentCache = sp.GetRequiredService<AgentCache>();

            var critique = sp.GetRequiredService<CritiqueExecutor>();

            var builder = new WorkflowBuilder(critique);

            foreach (var promptKey in agentCache.GetPromptsForAgent("Medium Preview Agent"))
            {
                var executor = new MediumPreviewExecutor($"{nameof(MediumPreviewExecutor)}_{promptKey}", promptKey, agentCache,
                    sp.GetRequiredService<ILogger<MediumPreviewExecutor>>());
                _context.MediumPreviews[promptKey] = new WorkflowNode<MediumPreviewResponse>();
                builder.BindExecutor(executor);
                builder.AddEdge(critique, executor);

                var enhancedText = $"The photograph titled '{_context.Photo!.Title}' is described as: {_context.Photo!.Description}. The medium to focus your research on is {promptKey}";

                string[] researchers = ["Research Specialist",
                "Marketing Expert",
                "Social Media Content Creator",
                "Email Marketing Specialist"];

                string[] areas = ["Product research", "Marketing research", "Social media strategy", "Email marketing strategy"];

                for (var idx = 0; idx < researchers.Length; idx++)
                {
                    var researcher = researchers[idx];
                    var area = areas[idx];
                    bool first = true;
                    ResearchExecutor? last = null;
                    var agent = agentCache[researcher];
                    foreach (var promptName in agentCache.GetPromptsForAgent(researcher))
                    {
                        var fullPrompt = $"{agentCache.GetPrompt(researcher, promptName)} {enhancedText}";
                        var researchStep = new ResearchExecutor(
                            $"{researcher}_{promptKey}_{promptName}",
                            promptKey,
                            area,
                            promptName,                            
                            agent,
                            fullPrompt,
                        sp.GetRequiredService<ILogger<ResearchExecutor>>());
                        builder.BindExecutor(researchStep);
                        if (first)
                        {
                            builder.AddEdge(executor, researchStep);
                            first = false;                   
                        }
                        else
                        {
                            builder.AddEdge(last!, researchStep);
                        }
                        last = researchStep;
                    }
                    fanIn.Add(last!);
                }
            }

            var writer = sp.GetRequiredService<GenerateReportExecutor>();

            builder.BindExecutor(writer);
            builder.AddFanInEdge(fanIn, writer);

            _workflow = builder.Build();
            _context.WorkflowDiagram = _workflow.ToMermaidString();

            _logger.LogDebug(@"Workflow for {photoid}:\n{workflow}", state, _workflow.ToMermaidString());

            await InProcessExecution.RunAsync(_workflow, _context!);
            _context!.Completed = true;
        }
    }
}

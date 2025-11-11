using ArtistTool.Domain.Agents;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.Logging;

namespace ArtistTool.Workflows
{
    public class ResearchExecutor(string id, string medium, string area, string topic, AIAgent agent, string prompt, ILogger<ResearchExecutor> logger) : ReflectingExecutor<ResearchExecutor>(id),
        IMessageHandler<MarketingWorkflowContext, MarketingWorkflowContext>
    {
        public async ValueTask<MarketingWorkflowContext> HandleAsync(MarketingWorkflowContext message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            logger.LogDebug("Starting Research Executor for Medium: {Medium} and photo {photoId} with agent {agent} and prompt {prompt}",
                agent.Name, message.Id, agent.Name, prompt);

            var data = new WorkflowNode<ResearchResponse>();
            message.RegisterResearch(data);

            data.Started = true;
            data.Result = new ResearchResponse
            {
                Area = area,
                Topic = topic,
                Medium = medium
            };

            var response = await agent.RunAsync(prompt, cancellationToken: cancellationToken);

            data.Result.Content = response.Text;
            data.Completed = true;

            return message;
        }
    }
}
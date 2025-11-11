using ArtistTool.Domain.Agents;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;

namespace ArtistTool.Workflows
{
    public class CritiqueExecutor(string id, AgentCache agents) : ReflectingExecutor<CritiqueExecutor>(id),
        IMessageHandler<MarketingWorkflowContext, MarketingWorkflowContext>
    {
        const string CRITIQUE_AGENT = "Photo Critique Agent";
        
        public async ValueTask<MarketingWorkflowContext> HandleAsync(
            MarketingWorkflowContext message, 
            IWorkflowContext context, 
            CancellationToken cancellationToken = default)
        {
            message.Critique.Started = true;

            var prompt = agents.GetPrompt(CRITIQUE_AGENT, "Critique");
            var promptContent = new TextContent(prompt);
            var dataContent = new DataContent(
                File.ReadAllBytes(message.Photo!.Path),
                message.Photo?.ContentType ?? "unknown");
            var promptForModel = new ChatMessage(ChatRole.User, [promptContent, dataContent]);
            var client = agents.GetChatClient(CRITIQUE_AGENT);
            var response = await client.GetResponseAsync<CritiqueResponse>(promptForModel, cancellationToken: cancellationToken);

            message.Critique.Result = response.Result;
            message.Critique.Completed = true;

            return message;
        }
    }
}
using ArtistTool.Domain;
using ArtistTool.Domain.Agents;
using ArtistTool.Intelligence;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ArtistTool.Workflows
{
    public class CritiqueWorkflow(
        IAIClientProvider provider, ILoggerFactory factory)
    {
        private ChatClientAgent? agent = null; 

        public async Task<ExecutorBinding> GetCritiqueExecutorAsync()
        {
            var executor = new ChatMessageExecutor(
               ProcessChatMessageAsync, "ChatMessageExecutor");
            
            var critiqueClient = provider.GetVisionClient();
            agent = new ChatClientAgent(critiqueClient, @"You are a photo critique agent responsible for critiquing photos and providing suggestions for improvement. Focus on the following aspects:

            - Composition
            - Visual weight
            - Technique
            - Impact
            - Creativity and originality
            - Summary (sum of the previous points plus final commentary)

            Rate each aspect on a scale of 1 to 10 and provide detailed feedback and actionable suggestions for improvement. Also provide a summary critique at the end.
            ", "Vision Critique Client", loggerFactory: factory);

            return executor;
        }

        private async ValueTask<ChatMessage> ProcessChatMessageAsync(IWorkflowContext context, ChatMessage cm)
        {

            var dcToProcess = cm.Contents.OfType<DataContent>().First(dc => dc.Name == nameof(Photograph));
            var prompt = new TextContent("Please provide a detailed critique of the attached photo, focusing on the aspects per your instructions. Rate each aspect on a scale of 1 to 10 and provide actionable suggestions for improvement.");
            var response = await agent!.RunAsync<CritiqueResponse>(new ChatMessage(ChatRole.User, [prompt, dcToProcess]));
            await context.YieldOutputAsync(response.Result);
            var dcToAdd = response.Result.AsSerializedChatParameter();
            return new ChatMessage(ChatRole.Assistant, [dcToAdd, .. cm.Contents]);
        }
    }
}

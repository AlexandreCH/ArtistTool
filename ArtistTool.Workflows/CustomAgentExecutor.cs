using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ArtistTool.Workflows
{
    /// <summary>
    /// Used for an agent with typed responses.
    /// </summary>
    /// <typeparam name="TResponseType"></typeparam>
    /// <param name="id"></param>
    /// <param name="agent"></param>
    /// <param name="factory"></param>
    public class CustomAgentExecutor<TResponseType>(string id, ChatClientAgent agent, ILoggerFactory factory) : ReflectingExecutor<CustomAgentExecutor<TResponseType>>(id), IMessageHandler<ChatMessage, TResponseType>
    {
        public async ValueTask<TResponseType> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            var logger = factory.CreateLogger<CustomAgentExecutor<TResponseType>>();
            logger.LogDebug("Handling message for agent {Agent} with return type {ResponseType}", agent.Name, typeof(TResponseType).Name);
            var result = await agent.RunAsync<TResponseType>(message, cancellationToken: cancellationToken);
            return result.Result;
        }
    }
}

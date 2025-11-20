using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;

namespace ArtistTool.Workflows
{
    /// <summary>
    /// A simple executor that takes and returns chat messages.
    /// </summary>
    /// <param name="handler">The handler to transform.</param>
    /// <param name="id"></param>
    public class ChatMessageExecutor(Func<IWorkflowContext, ChatMessage,ValueTask<ChatMessage>> handler,
        string id) : ReflectingExecutor<ChatMessageExecutor>(id), IMessageHandler<ChatMessage, ChatMessage>
    {
        public async ValueTask<ChatMessage> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
          => await handler(context, message);
    }       
}

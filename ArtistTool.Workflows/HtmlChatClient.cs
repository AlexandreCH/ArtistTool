using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ArtistTool.Workflows
{
    /// <summary>
    /// Chat client decorator that injects additional HTML/data attachments into the first user message
    /// without changing the original ordering required by downstream AI providers.
    /// </summary>
    public class HtmlChatClient(IChatClient original, Func<DataContent[]> attachments, ILoggerFactory factory) : IChatClient
    {
        private readonly ILogger logger = factory.CreateLogger<HtmlChatClient>();
        public void Dispose() => original.Dispose();

        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            // Materialize once to preserve ordering and allow in-place update.
            var list = messages as IList<ChatMessage> ?? messages.ToList();

            // Capture first system message text (if any) before potential mutation.
            var instructions = list.FirstOrDefault(m => m.Role == ChatRole.System)?.Text ?? "No instructions provided.";

            // Check if any tool messages exist in the conversation history.
            var hasToolMessages = list.Any(m => m.Role == ChatRole.Tool);

            // Find the last user message in the conversation.
            var lastUserIndex = -1;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Role == ChatRole.User)
                {
                    lastUserIndex = i;
                    break;
                }
            }

            string prompt = string.Empty;

            // Only inject attachments if:
            // 1. We found a user message
            // 2. There are no tool messages in the history (to avoid disrupting tool call sequences)
            // 3. The last user message is actually the last message (no assistant/tool responses after it)
            var canInject = lastUserIndex >= 0 && !hasToolMessages && lastUserIndex == list.Count - 1;

            if (canInject)
            {
                var originalUser = list[lastUserIndex];
                prompt = originalUser.Text;

                // Build updated user message including attachments, preserving existing contents.
                var newUser = new ChatMessage(ChatRole.User, [.. originalUser.Contents, .. attachments()]);
                list[lastUserIndex] = newUser;
            }

            // Forward the unchanged ordering to the underlying client to avoid violating message sequencing rules.
            var response = await original.GetResponseAsync(list, options, cancellationToken).ConfigureAwait(false);

            logger.LogInformation(@"HTML Client Request: 
Instructions: {Instructions},
Prompt: {Prompt},
Reply: {Reply}", instructions, prompt, response.RawRepresentation);
            return response;
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
            => original.GetService(serviceType, serviceKey);

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => original.GetStreamingResponseAsync(messages, options, cancellationToken);
    }
}

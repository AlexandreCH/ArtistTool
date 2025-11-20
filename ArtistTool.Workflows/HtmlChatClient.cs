using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ArtistTool.Workflows
{
    public class HtmlChatClient(IChatClient original, Func<DataContent[]> attachments, ILoggerFactory factory) : IChatClient
    {
        private readonly ILogger logger = factory.CreateLogger<HtmlChatClient>();
        public void Dispose() => original.Dispose();

        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var message = messages.First(cm => cm.Role == ChatRole.User);
            var otherMessages = messages.Except([message]);
            var instructions = messages.FirstOrDefault(cm => cm.Role == ChatRole.System)?.Text ?? "No instructions provided.";
            var prompt = message.Text;
            var newMessage = new ChatMessage(ChatRole.User, [.. message.Contents, .. attachments()]);
            ChatMessage[] modifiedMessages = [newMessage, ..otherMessages];
            var response = await (original.GetResponseAsync(modifiedMessages, options, cancellationToken));
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

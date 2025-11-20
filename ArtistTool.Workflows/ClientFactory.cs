using ArtistTool.Intelligence;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ArtistTool.Workflows
{
    /// <summary>
    /// Factory for creating and caching clients based on ClientCacheEntry configurations
    /// </summary>
    public class ClientFactory(
        IAIClientProvider provider,
        ILoggerFactory factory)
    {
        private readonly ILogger<ClientFactory> logger = factory.CreateLogger<ClientFactory>();
        private readonly Dictionary<string, IChatClient> _chatCache = [];
        private IImageGenerator? _imageClient;

        public IChatClient CreateClientWithOverrides(IChatClient client, string instructions, bool? supportTools = false)
        {
            var builder = new ChatClientBuilder(client);
            builder.ConfigureOptions(opts => 
            {
                if (supportTools == true)
                {
                    opts.AllowMultipleToolCalls = true;
                    opts.ToolMode = ChatToolMode.Auto;
                }
                
                if (!string.IsNullOrWhiteSpace(instructions))
                {
                    opts.Instructions = instructions;
                }
            });
            builder.UseLogging(factory);
         
            if (supportTools == true)
            {
                builder.UseFunctionInvocation(factory);
            }

            builder.UseOpenTelemetry(factory, configure: client => client.EnableSensitiveData = true);
            return builder.Build();
        }

        /// <summary>
        /// Gets a chat client by name
        /// </summary>
        public async Task<IChatClient> GetChatClientAsync(ChatClientType type, string? instructions = "", bool? useTools = false)
        {
            logger.LogDebug("Initializing chat client of type {ClientType}", $"{type}");

            var client = type == ChatClientType.Conversational ?
                   provider.GetConversationalClient() : provider.GetVisionClient();
            var enrichedClient = CreateClientWithOverrides(client, instructions ?? string.Empty, useTools);
            return client;
        }

        /// <summary>
        /// Gets an image generator by name
        /// </summary>
        public IImageGenerator GetImageGenerator() => _imageClient ??= provider.GetImageClient();
    }
}

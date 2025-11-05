using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using OpenAIChat = OpenAI.Chat;
using MEAIChatMessage = Microsoft.Extensions.AI.ChatMessage;
using MEAIChatCompletion = Microsoft.Extensions.AI.ChatCompletion;
using MEAIChatFinishReason = Microsoft.Extensions.AI.ChatFinishReason;
using MEAIStreamingUpdate = Microsoft.Extensions.AI.StreamingChatCompletionUpdate;

namespace ArtistTool.Intelligence
{
    public class AzureOpenAIClientProvider : IAIClientProvider
    {
        private readonly IChatClient _conversationalClient;
        private readonly IChatClient _visionClient;

        /// <summary>
        /// Creates an Azure OpenAI client provider using Managed Identity (DefaultAzureCredential)
        /// </summary>
        /// <param name="endpoint">Azure OpenAI endpoint URL (e.g., https://your-resource.openai.azure.com/)</param>
        /// <param name="conversationalDeployment">Deployment name for conversational model (e.g., gpt-4o)</param>
        /// <param name="visionDeployment">Deployment name for vision model (e.g., gpt-4o)</param>
        public AzureOpenAIClientProvider(
            string endpoint, 
            string conversationalDeployment = "gpt-4o", 
            string visionDeployment = "gpt-4o")
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("Azure OpenAI endpoint is required", nameof(endpoint));
            }

            if (string.IsNullOrWhiteSpace(conversationalDeployment))
            {
                throw new ArgumentException("Conversational deployment name is required", nameof(conversationalDeployment));
            }

            if (string.IsNullOrWhiteSpace(visionDeployment))
            {
                throw new ArgumentException("Vision deployment name is required", nameof(visionDeployment));
            }

            // Use DefaultAzureCredential for managed identity support
            // This supports: Managed Identity, Azure CLI, Visual Studio, Environment Variables, etc.
            var credential = new DefaultAzureCredential();
            
            var azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);
            
            // Get ChatClient instances from Azure OpenAI
            var conversationalChatClient = azureClient.GetChatClient(conversationalDeployment);
            var visionChatClient = azureClient.GetChatClient(visionDeployment);
            
            // Wrap with adapter
            _conversationalClient = new OpenAIChatClientAdapter(conversationalChatClient);
            _visionClient = new OpenAIChatClientAdapter(visionChatClient);
        }

        /// <summary>
        /// Creates an Azure OpenAI client provider with a specific credential
        /// </summary>
        /// <param name="endpoint">Azure OpenAI endpoint URL</param>
        /// <param name="credential">Azure credential (e.g., ManagedIdentityCredential, DefaultAzureCredential)</param>
        /// <param name="conversationalDeployment">Deployment name for conversational model</param>
        /// <param name="visionDeployment">Deployment name for vision model</param>
        public AzureOpenAIClientProvider(
            string endpoint,
            Azure.Core.TokenCredential credential,
            string conversationalDeployment = "gpt-4o",
            string visionDeployment = "gpt-4o")
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("Azure OpenAI endpoint is required", nameof(endpoint));
            }

            if (credential == null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            var azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);
            
            var conversationalChatClient = azureClient.GetChatClient(conversationalDeployment);
            var visionChatClient = azureClient.GetChatClient(visionDeployment);
            
            _conversationalClient = new OpenAIChatClientAdapter(conversationalChatClient);
            _visionClient = new OpenAIChatClientAdapter(visionChatClient);
        }

        public IChatClient GetConversationalClient() => _conversationalClient;

        public IChatClient GetVisionClient() => _visionClient;
    }

    /// <summary>
    /// Adapter to wrap OpenAI ChatClient to Microsoft.Extensions.AI IChatClient
    /// </summary>
    internal class OpenAIChatClientAdapter : IChatClient
    {
        private readonly OpenAIChat.ChatClient _chatClient;

        public OpenAIChatClientAdapter(OpenAIChat.ChatClient chatClient)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        }

        public ChatClientMetadata Metadata => new("Azure OpenAI", modelId: "gpt-4o");

        public async Task<MEAIChatCompletion> CompleteAsync(IList<MEAIChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var messages = chatMessages.Select(m => ToChatMessage(m)).ToList();
            
            var chatCompletionOptions = new OpenAIChat.ChatCompletionOptions();
            if (options?.Temperature.HasValue == true)
            {
                chatCompletionOptions.Temperature = (float)options.Temperature.Value;
            }
            if (options?.MaxOutputTokens.HasValue == true)
            {
                chatCompletionOptions.MaxOutputTokenCount = options.MaxOutputTokens.Value;
            }
            if (options?.TopP.HasValue == true)
            {
                chatCompletionOptions.TopP = (float)options.TopP.Value;
            }

            var response = await _chatClient.CompleteChatAsync(messages, chatCompletionOptions, cancellationToken);
            
            return ToMEAIChatCompletion(response.Value);
        }

        public IAsyncEnumerable<MEAIStreamingUpdate> CompleteStreamingAsync(IList<MEAIChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Streaming is not yet implemented for this adapter.");
        }

        public TService? GetService<TService>(object? key = null) where TService : class
        {
            return this as TService;
        }

        public object? GetService(Type serviceType, object? key = null)
        {
            return serviceType.IsInstanceOfType(this) ? this : null;
        }

        public void Dispose()
        {
            // ChatClient doesn't need disposal
        }

        private static OpenAIChat.ChatMessage ToChatMessage(MEAIChatMessage message)
        {
            return message.Role.Value switch
            {
                "system" => new OpenAIChat.SystemChatMessage(GetTextContent(message)),
                "user" => CreateUserMessage(message),
                "assistant" => new OpenAIChat.AssistantChatMessage(GetTextContent(message)),
                _ => throw new ArgumentException($"Unsupported role: {message.Role.Value}")
            };
        }

        private static OpenAIChat.UserChatMessage CreateUserMessage(MEAIChatMessage message)
        {
            var contentParts = new List<OpenAIChat.ChatMessageContentPart>();
            
            foreach (var content in message.Contents)
            {
                if (content is TextContent textContent)
                {
                    contentParts.Add(OpenAIChat.ChatMessageContentPart.CreateTextPart(textContent.Text));
                }
                else if (content is ImageContent imageContent)
                {
                    var imageBytes = imageContent.Data?.ToArray() ?? throw new InvalidOperationException("Image content has no data");
                    contentParts.Add(OpenAIChat.ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageBytes), imageContent.MediaType));
                }
            }
            
            return new OpenAIChat.UserChatMessage(contentParts);
        }

        private static string GetTextContent(MEAIChatMessage message)
        {
            var textContent = message.Contents.OfType<TextContent>().FirstOrDefault();
            return textContent?.Text ?? string.Empty;
        }

        private static MEAIChatCompletion ToMEAIChatCompletion(OpenAIChat.ChatCompletion completion)
        {
            var choice = completion.Content.FirstOrDefault();
            if (choice == null)
            {
                throw new InvalidOperationException("No content returned from Azure OpenAI");
            }

            var message = new MEAIChatMessage(
                new ChatRole("assistant"),
                choice.Text
            );

            return new MEAIChatCompletion(message)
            {
                CompletionId = completion.Id,
                ModelId = completion.Model,
                FinishReason = ToFinishReason(completion.FinishReason),
                Usage = new UsageDetails
                {
                    InputTokenCount = completion.Usage?.InputTokenCount,
                    OutputTokenCount = completion.Usage?.OutputTokenCount,
                    TotalTokenCount = completion.Usage?.TotalTokenCount
                }
            };
        }

        private static MEAIChatFinishReason? ToFinishReason(OpenAIChat.ChatFinishReason? finishReason)
        {
            return finishReason switch
            {
                OpenAIChat.ChatFinishReason.Stop => MEAIChatFinishReason.Stop,
                OpenAIChat.ChatFinishReason.Length => MEAIChatFinishReason.Length,
                OpenAIChat.ChatFinishReason.ContentFilter => MEAIChatFinishReason.ContentFilter,
                OpenAIChat.ChatFinishReason.ToolCalls => MEAIChatFinishReason.ToolCalls,
                _ => null
            };
        }
    }
}

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ArtistTool.Intelligence
{
    public class AzureOpenAIClientProvider : IAIClientProvider
    {
        private readonly IChatClient _conversationalClient;
        private readonly IChatClient _visionClient;
        private readonly IImageGenerator _imageClient;

        /// <summary>
        /// Creates an Azure OpenAI client provider using Managed Identity (DefaultAzureCredential)
        /// with logging and OpenTelemetry support
        /// </summary>
        /// <param name="endpoint">Azure OpenAI endpoint URL (e.g., https://your-resource.openai.azure.com/)</param>
        /// <param name="conversationalDeployment">Deployment name for conversational model (e.g., gpt-4o)</param>
        /// <param name="visionDeployment">Deployment name for vision model (e.g., gpt-4o)</param>
        /// <param name="imageDeployment">Deployment name for image model (e.g., gpt-image-1)</param>
        /// <param name="loggerFactory">Optional logger factory for enabling logging on chat clients</param>
        public AzureOpenAIClientProvider(
            string endpoint, 
            string conversationalDeployment = "gpt-4o", 
            string visionDeployment = "gpt-4o",
            string imageDeployment = "gpt-image-1",
            ILoggerFactory? loggerFactory = null)
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

            if (string.IsNullOrWhiteSpace(imageDeployment))
            {
                throw new ArgumentException("Image deployment name is required", nameof(imageDeployment));
            }

            // Use DefaultAzureCredential for managed identity support
            // This supports: Managed Identity, Azure CLI, Visual Studio, Environment Variables, etc.
            var credential = new DefaultAzureCredential();
            
            var azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);

            // Get ChatClient instances from Azure OpenAI and enhance with logging & telemetry
            var conversationalChatClient = azureClient.GetChatClient(conversationalDeployment);
            var visionChatClient = azureClient.GetChatClient(visionDeployment);
            var imageClient = azureClient.GetImageClient(imageDeployment);

            // Build enhanced clients with logging and OpenTelemetry using ChatClientBuilder
            _conversationalClient = BuildEnhancedChatClient(
                conversationalChatClient.AsIChatClient(), 
                "conversational", 
                loggerFactory);
            
            _visionClient = BuildEnhancedChatClient(
                visionChatClient.AsIChatClient(), 
                "vision", 
                loggerFactory);
            
            _imageClient = imageClient.AsIImageGenerator();
        }

        private static IChatClient BuildEnhancedChatClient(
            IChatClient innerClient, 
            string clientName, 
            ILoggerFactory? loggerFactory)
        {
            var builder = new ChatClientBuilder(innerClient);

            // Add logging if logger factory is provided
            if (loggerFactory is not null)
            {
                builder.UseLogging(loggerFactory);
            }

            // Add OpenTelemetry tracing and metrics
            // Note: The sourceName parameter in UseOpenTelemetry may not actually create activities under that name
            // Microsoft.Extensions.AI uses its own internal activity source
            // We keep this for configuration purposes, but the actual traces will appear under "Microsoft.Extensions.AI"
            builder.UseOpenTelemetry(
                configure: options =>
                {
                    // Enable detailed telemetry including prompts and responses in dev
                    options.EnableSensitiveData = true; // Set to false in prod to protect PII 
                });

            // Add function invocation capability (for potential future tool use)
            builder.UseFunctionInvocation();

            return builder.Build();
        }

        public IChatClient GetConversationalClient() => _conversationalClient;

        public IChatClient GetVisionClient() => _visionClient;

        public IImageGenerator GetImageClient() => _imageClient;
    }
}

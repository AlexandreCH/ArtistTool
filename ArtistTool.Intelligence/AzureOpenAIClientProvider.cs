using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;

namespace ArtistTool.Intelligence
{
    public class AzureOpenAIClientProvider : IAIClientProvider
    {
        private readonly IChatClient _conversationalClient;
        private readonly IChatClient _visionClient;
        private readonly IImageGenerator _imageClient;

        /// <summary>
        /// Creates an Azure OpenAI client provider using Managed Identity (DefaultAzureCredential)
        /// </summary>
        /// <param name="endpoint">Azure OpenAI endpoint URL (e.g., https://your-resource.openai.azure.com/)</param>
        /// <param name="conversationalDeployment">Deployment name for conversational model (e.g., gpt-4o)</param>
        /// <param name="visionDeployment">Deployment name for vision model (e.g., gpt-4o)</param>
        public AzureOpenAIClientProvider(
            string endpoint, 
            string conversationalDeployment = "gpt-4o", 
            string visionDeployment = "gpt-4o",
            string imageDeployment = "gpt-image-1")
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

            // Get ChatClient instances from Azure OpenAI
            var conversationalChatClient = azureClient.GetChatClient(conversationalDeployment);
            var visionChatClient = azureClient.GetChatClient(visionDeployment);
            var imageClient = azureClient.GetImageClient(imageDeployment);
            
            // Wrap with adapter
            _conversationalClient = conversationalChatClient.AsIChatClient();
            _visionClient = visionChatClient.AsIChatClient();
            _imageClient = imageClient.AsIImageGenerator();
        }        

        public IChatClient GetConversationalClient() => _conversationalClient;

        public IChatClient GetVisionClient() => _visionClient;

        public IImageGenerator GetImageClient() => _imageClient;
    }
}

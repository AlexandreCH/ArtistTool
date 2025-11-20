using ArtistTool.Domain;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace ArtistTool.Workflows
{
    public class MediumPreviewExecutor(string medium, string prompt, ILoggerFactory loggerFactory, ClientFactory factory) : ReflectingExecutor<MediumPreviewExecutor>($"{nameof(MediumPreviewExecutor)}_{medium}".Replace(" ", "_")),
        IMessageHandler<ChatMessage, ChatMessage>
    {
        const string MEDIUM_AGENT = "Medium Preview Agent";

        private ILogger logger = loggerFactory.CreateLogger <MediumPreviewExecutor>();

        public async ValueTask<ChatMessage> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogDebug("Medium preview executor invoked for medium {Medium} with message payload: {Message}",
                    medium,
                    message.AsDebugString());

                logger.LogDebug("Starting {AgentName} for Medium: {Medium}", MEDIUM_AGENT, medium);

                var client = factory.GetImageGenerator();

                var photoImage =
                    message.Contents.OfType<DataContent>().First(f => f.Name == nameof(Photograph));

                var originalImage = new DataContent(photoImage.Data, photoImage.MediaType);

                var response = await client.GenerateAsync(new ImageGenerationRequest
                {
                    Prompt = prompt,
                    OriginalImages = [originalImage]
                }, cancellationToken: cancellationToken);

                logger.LogDebug("Prompt returned.");

                var data = response.Contents.OfType<DataContent>().FirstOrDefault();

                if (data is null || data.Data.Length == 0)
                {
                    throw new InvalidDataException("DataContent not generated.");
                }

                data.Name = $"Medium preview: {medium}";
                
                await context.YieldOutputAsync(new MediumPreviewResponse(medium, data));
                return new ChatMessage(ChatRole.System,
                    [data, ..message.Contents]);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Image generation failed.");
                throw;
            }
        }
    }
}
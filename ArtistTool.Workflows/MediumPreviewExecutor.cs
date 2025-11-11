using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ArtistTool.Workflows
{
    public class MediumPreviewExecutor(string id, string medium, AgentCache agents, ILogger<MediumPreviewExecutor> logger) : ReflectingExecutor<CritiqueExecutor>(id),
        IMessageHandler<MarketingWorkflowContext, MarketingWorkflowContext>
    {
        const string MEDIUM_AGENT = "Medium Preview Agent";

        public async ValueTask<MarketingWorkflowContext> HandleAsync(MarketingWorkflowContext message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            logger.LogDebug("Starting MediumPreviewExecutor for Medium: {Medium} and photo {photoId}", medium, message.Id);

            var directory = message.BaseDirectory;
            var filename = Path.GetFileNameWithoutExtension(message.Photo!.Path);
 
            var resp = message.MediumPreviews[medium];
            
            if (resp.Completed && resp.Result is not null && File.Exists(resp.Result.ImagePath))
            {
                logger.LogDebug("MediumPreviewExecutor found existing preview for Medium: {Medium} and photo {photoId}", medium, message.Id);
                
                return message;
            }
            else if (resp.Started)
            {
                logger.LogDebug("MediumPreviewExecutor found existing preview for Medium: {Medium} and photo {photoId}", medium, message.Id);
                return message;
            }

            resp.Started = true;
      
            var prompt = agents.GetPrompt(MEDIUM_AGENT, medium);
            
            var dataContent = new DataContent(
                File.ReadAllBytes(message.Photo!.Path),
                message.Photo?.ContentType ?? "unknown");
            
            var client = agents.GetImageClient(MEDIUM_AGENT);
            
            var response = await client.GenerateAsync(new ImageGenerationRequest
            {
                Prompt = prompt,
                OriginalImages = [dataContent]
            }, cancellationToken: cancellationToken);
                
            var data = response.Contents.OfType<DataContent>().FirstOrDefault();

            if (data is null || data.Data.Length == 0)
            {
                logger.LogError("MediumPreviewExecutor failed to generate preview for Medium: {Medium} and photo {photoId}", medium, message.Id);
                resp.Completed = true;
                return message;
            }

            var extension = data?.MediaType switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                _ => ".img"
            };

            var mediumPath = Path.Combine(directory!, $"{filename}_{medium}{extension}");

            logger.LogDebug("Saving medium {Medium} for photo {photoId} to location {path}",
                medium, message.Id, mediumPath);

            File.WriteAllBytes(mediumPath, data!.Data.ToArray());
            
            message.ImageRegistry.RegisterImage(Path.GetFileName(mediumPath),  $"In the style of {medium}: {message.Photo!.Title}", $"A rendition of the image in {medium}");

            resp.Result = new MediumPreviewResponse
            {
                Medium = medium,
                ImagePath = mediumPath
            };

            resp.Completed = true;
            return message;
        }
    }
}
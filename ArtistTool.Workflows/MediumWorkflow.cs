using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ArtistTool.Workflows
{
    /// <summary>
    /// Fans out to various mediums, then fans back in.
    /// </summary>
    /// <param name="messageSystem"></param>
    /// <param name="photoDatabase"></param>
    /// <param name="factory"></param>
    /// <param name="clientFactory"></param>
    public class MediumWorkflow(ILoggerFactory factory, ClientFactory clientFactory, ResearchFactory research)
    {
         public async Task<(ExecutorBinding[] fanOut, ExecutorBinding[] fanIn)> GetMediumExecutorsAsync(WorkflowBuilder builder)
        {
            var mediums = new Dictionary<string, string>
            {
                { "Metal", "Show how the image would look like on a glossy metal ChromaLux dye sublimated print with a float mount." },
                { "Canvas", "Show how the image would look like on a stretched canvas print, wrapped around the edges. Render it on a white wall with the camera positioned to view at about a 45-degree angle to show the thickness of the edges and highlight the wrapped sides." },
                { "Acrylic", "Render the image mounted on a wall in an acrylic mount. This is a glasslike cover so it should be reflective." },
                { "Framed photo", "Show the photo on matte traditional photo paper with a sleek, dark wood frame." }
            };
             
            var nodes = new List<ExecutorBinding>();
            var fanInNodes = new List<ExecutorBinding>();

            foreach (var medium in mediums.Keys)
            {
                var mediumPreview = new MediumPreviewExecutor(medium, mediums[medium], factory, clientFactory);
                builder.BindExecutor(mediumPreview).WithOutputFrom(mediumPreview); ;
                nodes.Add(mediumPreview);
                var researchNodes = research.GetResearchNodes(medium, builder, mediumPreview);
                var researchAggregator = new AggregatingExecutor<ChatMessage, ChatMessage>(Guid.NewGuid().ToString(),
                    AggregateChat);
                builder.BindExecutor(researchAggregator);
                builder.AddFanInEdge(researchNodes, researchAggregator);
                fanInNodes.Add(researchAggregator);
            }

            return ([.. nodes], [..fanInNodes]);
        }

        private ChatMessage? AggregateChat(ChatMessage? message1, ChatMessage message2)
        {
            if (message1 is null)
            {
                return message2;
            }
            return new ChatMessage(ChatRole.Assistant, [.. message1.Contents, .. message2.Contents]);
        }
    }
}

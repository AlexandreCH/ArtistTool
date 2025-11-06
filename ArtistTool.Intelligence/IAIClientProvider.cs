using Microsoft.Extensions.AI;

namespace ArtistTool.Intelligence
{
    public interface IAIClientProvider
    {
        IChatClient GetConversationalClient();
        IChatClient GetVisionClient();
        IImageGenerator GetImageClient();
    }
}

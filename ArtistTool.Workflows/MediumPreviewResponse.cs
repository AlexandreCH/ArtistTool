using Microsoft.Extensions.AI;

namespace ArtistTool.Workflows
{
    public class MediumPreviewResponse(string medium, DataContent preview)
    {
        public string Medium { get; } = medium;
        public DataContent Preview { get; } = preview;
    }
}

namespace ArtistTool.Workflows.Messages
{
    public class JobStartedFor<TOutput>(string title)
    {
        public string Title { get; } = title;
    }
}

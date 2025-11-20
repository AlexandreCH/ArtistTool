namespace ArtistTool.Workflows.Messages
{
    public class JobEndedFor<TOutput>(string id, TOutput result)
    {
        public string Id { get; } = id;
        public TOutput Result { get; } = result;
    }
}

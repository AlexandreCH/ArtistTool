namespace ArtistTool.Workflows.Messages
{
    public class WorkflowRequested<T>(string name, T payload)
    {
        public string Name { get; } = name;
        public T Payload { get; } = payload;
    }
}

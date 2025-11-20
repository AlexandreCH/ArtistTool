namespace ArtistTool.Domain.Agents
{
    public class ResearchResponse : IMediumScopedResult
    {
        public string Medium { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string DetailedAnswer { get; set; } = string.Empty;
    }
}

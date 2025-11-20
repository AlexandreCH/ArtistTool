namespace ArtistTool.Workflows.Messages
{
    public class ReportFinished(string id, MarketReport report, string path)
    {
        public string Id => id; 
        public string Path => path;
        public MarketReport Report => report;
    }
}

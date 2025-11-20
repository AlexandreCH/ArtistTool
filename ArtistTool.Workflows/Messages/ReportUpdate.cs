namespace ArtistTool.Workflows.Messages
{
    public class ReportUpdate(string id, MarketReport report)
    {
        public string Id { get; set; } = id;
        public MarketReport Report { get; set; } = report;
    }
}

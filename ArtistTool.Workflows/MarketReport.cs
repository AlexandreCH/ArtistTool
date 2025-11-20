using ArtistTool.Domain;
using ArtistTool.Domain.Agents;

namespace ArtistTool.Workflows
{
    public class MarketReport(Photograph photo, int reportId) : BaseObservable
    {
        private double CritiqueValue => Critique is null ? 0 : 1;
        private double MediumReportsValue => MediumReports.Length * 1.0;
        private double Perct => (MediumReports.Sum(m => m.Percent) + CritiqueValue) / (MediumReportsValue + 1);
        public int Pct => (int)(Perct * 100.0);
        public string Workflow { get; set; } = "";
        public int ReportWritingPct { get; set; } = 0;
        public bool Done { get; set; } = false;
        public bool AnalysisDone { get; set; } = false;
        public DateTime StartDate { get; set; } = DateTime.Now;
        public string Id { get; private set; } = photo.Id;
        public int ReportId { get; private set; } = reportId;
        public string Status { get; set; } = "Initializing"; 
        public Photograph? Photo { get; private set; } = photo;
        public CritiqueResponse? Critique { get; set; }
        public MediumReport[] MediumReports { get; set; } = [];
    }
}

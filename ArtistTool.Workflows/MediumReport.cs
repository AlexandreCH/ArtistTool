using ArtistTool.Domain.Agents;
using Microsoft.Extensions.AI;

namespace ArtistTool.Workflows
{
    public class MediumReport(string medium)
    {
        private double PreviewValue => MediumPreview is null ? 0 : 1;
        private double ResearchResponsesValue => ResearchResponses.Length > 0 ? 1 : 0;
        private double PriceResponseValue => PriceResponse is null ? 0 : 1;
        private double SocialsValue => Socials.Length > 0 ? 1 : 0;
        private double ProductResearchValue => ProductResearchResponse is null ? 0 : 1;
        private double MarketingCampaignValue => MarketingCampaignResponse is null ? 0 : 1;

        private double[] Values => 
            [PreviewValue, ResearchResponsesValue, PriceResponseValue, SocialsValue,  ProductResearchValue, MarketingCampaignValue];

        public double Percent => Values.Sum() / Values.Length;
        public string Medium { get; } = medium;
        public DateTime ReportStart { get; private set; } = DateTime.Now;
        public DataContent? MediumPreview { get; set; }
        public ResearchResponse[] ResearchResponses { get; set; } = [];
        public PriceResponse? PriceResponse { get; set; }
        public SocialMediaChannelResponse[] Socials { get; set; } = [];
        public ProductResearchResponse? ProductResearchResponse { get; set; }
        public MarketingCampaignResponse? MarketingCampaignResponse { get; set; }

    }
}

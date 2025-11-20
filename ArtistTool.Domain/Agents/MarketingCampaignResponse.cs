using System.ComponentModel;

namespace ArtistTool.Domain.Agents
{
    [Description("A marketing campaign plan for a specific medium.")]
    public class MarketingCampaignResponse : IMediumScopedResult
    {
        [Description("The medium being evaluated for the marketing campaign, such as canvas or metal.")]
        public string Medium { get; set; } = string.Empty;

        [Description("The primary segment you recommend this is marketed to.")]
        public string PrimarySegment { get; set; } = string.Empty;

        [Description("The secondary segment you recommend this is marketed to.")]
        public string SecondarySegment { get; set; } = string.Empty;

        [Description("A concise one-line statement that captures the essence of the marketing campaign's positioning.")]
        public string PositionStatement { get; set; } = string.Empty;

        [Description("A list of key message pillars that support the marketing campaign.")]
        public string[] MessagePillars { get; set; } = [];
        [Description("A list of recommended channels for the marketing campaign. Start with the channel name, include if it's owner, earned, or paid, and describe the pros and cons of the channel.")]        
        public string[] Channels { get; set; } = [];

        [Description("A list of materials to prepare such as product brochures and fliers.")]
        public string[] Materials { get; set; } = [];

        [Description("A timeline outlining the key phases and milestones of the marketing campaign. Start with a sequence followed by the time period in days or weeks (such as: 1. days 1 - 5 or 1. weeks 1 - 4) ")]
        public string[] Campaign {  get; set; } = [];
    }
}

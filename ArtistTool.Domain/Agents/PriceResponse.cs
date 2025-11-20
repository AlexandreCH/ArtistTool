    using System.ComponentModel;

namespace ArtistTool.Domain.Agents
{
    public class PriceResponse : IMediumScopedResult
    {
        [Description("The medium being evaluated for pricing, such as canvas or metal.")]
        public string Medium { get; set; } = string.Empty;

        [Description("Estimated cost to produce a print with this medium in a smaller size of 8 inches x 10 inches.")]
        public decimal SmallSizeEstimatedCost { get; set; }
        [Description("Recommended price to sell a print with this medium in a smaller size of 8 inches x 10 inches. Set to 0 if not recommended.")]
        public decimal SmallSizeRecommendedPrice { get; set; }

        [Description("Estimated cost to produce a print with this medium in a medium size of 16 inches x 24 inches.")]
        public decimal MediumSizeEstimatedCost { get; set; }
        [Description("Recommended price to sell a print with this medium in a medium size of 16 inches x 24 inches. Set to 0 if not recommended.")]
        public decimal MediumSizeRecommendedPrice { get; set; }

        [Description("Estimated cost to produce a print with this medium in a large size of 30 inches x 40 inches.")]
        public decimal LargeSizeEstimatedCost { get; set; }
        [Description("Recommended price to sell a print with this medium in a large size of 30 inches x 40 inches. Set to 0 if not recommended.")]
        public decimal LargeSizeRecommendedPrice { get; set; }

        [Description("A brief executive summary of the pricing analysis.")]
        public string ExecutiveSummary { get; set; } = string.Empty;

        [Description("Market comparables used to determine pricing recommendations.")]
        public string MarketComps { get; set; } = string.Empty;

        [Description("Detailed rationale behind the pricing recommendations.")]
        public string DetailedReport { get; set; } = string.Empty;
    }
}

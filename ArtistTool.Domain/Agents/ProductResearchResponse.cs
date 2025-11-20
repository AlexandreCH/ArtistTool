using System.ComponentModel;

namespace ArtistTool.Domain.Agents
{
    public class ProductResearchResponse : IMediumScopedResult
    {
        [Description("The medium being evaluated for the product, such as canvas or metal.")]
        public string Medium { get; set; } = string.Empty;
        [Description("The title of the specific product, including photograph title, size, and medium.")]
        public string ProductTitle { get; set; } = string.Empty;
        [Description("A compelling one sentence product description that highlights the unique features and benefits of the print.")]
        public string Sizzle { get; set; } = string.Empty;
        [Description("A detailed ad copy that provides an engaging narrative about the photograph and medium, ending with a powerful call to action to incite potential buyers.")]
        public string DetailedAdCopy { get; set; } = string.Empty;

        [Description("A list of recommended keywords to use for search engine optimization (SEO) to improve product visibility online.")]
        public string[] SearchEngineKeywords { get; set; } = [];

    }
}

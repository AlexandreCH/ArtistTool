using System.ComponentModel;

namespace ArtistTool.Domain.Agents
{
    [Description("A social media channel for marketing.")]
    public class ChannelDetail
    {
        [Description("The name of the social media channel/platform.")]
        public string Name { get; set; } = string.Empty;

        [Description("Why this channel is being considered for marketing the piece.")]
        public string Fit { get; set; } = string.Empty;

        [Description("What content/format should be broadcast here?")]
        public string Format { get; set; } = string.Empty;

        [Description("Popular relevant hashtags and/or keywords or tags for the channel.")]
        public string[] HashTags { get; set; } = [];
    }
}

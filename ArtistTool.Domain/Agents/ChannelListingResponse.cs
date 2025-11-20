using System.ComponentModel;

namespace ArtistTool.Domain.Agents
{
    public class ChannelListingResponse
    {
        [Description("The medium this channel list is for.")]
        public string Medium { get; set; } = string.Empty;

        [Description("This list of social media channels recommended.")]
        public ChannelDetail[] Channels { get; set; } = [];
    }
}

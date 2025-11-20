using System.ComponentModel;

namespace ArtistTool.Domain.Agents
{
    public class SocialMediaChannelResponse : IMediumScopedResult
    {
        [Description("The medium being evaluated for the social media channel, such as canvas or metal.")]
        public string Medium { get; set; } = string.Empty;

        [Description("The name of the social media channel being evaluated, such as Instagram or Facebook.")]
        public string ChannelName { get; set; } = string.Empty;

        [Description("The type of campaign for the social media channel, such as owned, earned, or paid.")]
        public string ChannelType { get; set; } = string.Empty;

        [Description("Rank is between 0 and 100 with 0 being completely ineffective channel and 100 being the best possible fit.")]
        public int Rank { get; set; }

        [Description("Summary of the benefits of going with this channel.")]
        public string Pros { get; set; } = string.Empty;

        [Description("Summary of the drawbacks of going with this channel.")]
        public string Cons { get; set; } = string.Empty;

        [Description("A list of example posts that would be effective on this channel.")]
        public string[] Posts { get; set; } = [];

        [Description("Best practices including size, attachments, and timing for posting to this channel.")]
        public string BestPractices { get; set; } = string.Empty;

        [Description("A list of relevant hashtags to include in posts on this channel to increase visibility and engagement.")]
        public string[] HashTags { get; set; } = [];
    }
}

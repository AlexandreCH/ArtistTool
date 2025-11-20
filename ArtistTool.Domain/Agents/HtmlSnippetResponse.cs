using System.ComponentModel;

namespace ArtistTool.Domain.Agents
{
    public class HtmlSnippetResponse
    {
        [Description("The HTML to embed in the web page.")]
        public string Html { get; set; } = string.Empty;

        [Description("New CSS introduced as the result of this snippet, not including existing CSS that was already passed in.")]
        public string NewCss { get; set; } = string.Empty;

        [Description("Optional commentary.")]
        public string Commentary { get; set; } = string.Empty;
    }
}

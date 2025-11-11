using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ArtistTool.Workflows
{
    public class AgentCacheEntry
    {
        public string AgentName { get; set; } = string.Empty;

        public string this[string promptKey]
        {
            get => Prompts.TryGetValue(promptKey, out string? value) ? value : string.Empty;
            set => Prompts[promptKey] = value;
        }

        public required AIAgent Agent { get; set; }
        public IChatClient? ChatClient { get; set; }
        public IImageGenerator? ImageGenerator { get; set; }

        public IDictionary<string, string> Prompts {  get; set; } = new Dictionary<string, string>();   
    }
}

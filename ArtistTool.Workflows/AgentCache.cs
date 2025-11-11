using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ArtistTool.Workflows
{
    public class AgentCache
    {
        private readonly Dictionary<string, AgentCacheEntry> _cache = [];

        public IImageGenerator GetImageClient(string agentName)
        {
            if (_cache.TryGetValue(agentName, out AgentCacheEntry? entry))
            {
                if (entry.ImageGenerator != null)
                {
                    return entry.ImageGenerator;
                }
                else
                {
                    throw new InvalidOperationException($"Agent '{agentName}' does not have an associated ImageGenerator.");
                }
            }
            else
            {
                throw new KeyNotFoundException($"Agent '{agentName}' not found in cache.");
            }
        }

        public IChatClient GetChatClient(string agentName)
        {
            if (_cache.TryGetValue(agentName, out AgentCacheEntry? entry))
            {
                if (entry.ChatClient != null)
                {
                    return entry.ChatClient;
                }
                else
                {
                    throw new InvalidOperationException($"Agent '{agentName}' does not have an associated ChatClient.");
                }
            }
            else
            {
                throw new KeyNotFoundException($"Agent '{agentName}' not found in cache.");
            }
        }

        public AIAgent this[string agentName]
        {
            get => _cache.TryGetValue(agentName, out AgentCacheEntry? entry) ? entry.Agent
                : throw new KeyNotFoundException($"Agent '{agentName}' not found in cache.");
        }

        public string[] GetPromptsForAgent(string agentName)
        {
            if (_cache.TryGetValue(agentName, out AgentCacheEntry? entry))
            {
                return [.. entry.Prompts.Keys];
            }
            else
            {
                throw new KeyNotFoundException($"Agent '{agentName}' not found in cache.");
            }
        }

        public string GetPrompt(string agentName, string promptKey)
        {
            if (_cache.TryGetValue(agentName, out AgentCacheEntry? entry))
            {
                if (entry.Prompts.TryGetValue(promptKey, out string? prompt))
                {
                    return prompt;
                }
                else
                {
                    throw new KeyNotFoundException($"Prompt key '{promptKey}' not found for agent '{agentName}'.");
                }
            }
            else
            {
                throw new KeyNotFoundException($"Agent '{agentName}' not found in cache.");
            }
        }

        public void AddAgent(AgentCacheEntry entry)
        {
            _cache[entry.AgentName] = entry;
        }
    }
}

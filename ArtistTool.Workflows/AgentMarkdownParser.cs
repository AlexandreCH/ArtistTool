using ArtistTool.Intelligence;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ArtistTool.Workflows
{
    public class AgentMarkdownParser(
        AgentCache agentCache,
        IAIClientProvider client,
        ILoggerFactory factory,
        ILogger<AgentMarkdownParser> logger)
    {
        private const string agentMark = "## ";
        private const string promptMark = "### ";

        private AgentCacheEntry MakeAgent(string name, string instructions, IDictionary<string, string> promptsForAgent)
        {
            logger.LogDebug("Creating agent from markdown: {AgentName}", name);

            var agentType = name.Split(' ', StringSplitOptions.RemoveEmptyEntries
                | StringSplitOptions.TrimEntries)[0];

            name = name[(agentType.Length)..].Trim();

            logger.LogDebug("Agent type: {AgentType}, Agent name: {AgentName}, Prompt count: {PromptCount}",
                agentType, name, promptsForAgent.Count);

            AIAgent? agent;
            IChatClient? chatClientUsed = null;
            IImageGenerator? imageGenerator = null;

            switch (agentType.ToLowerInvariant())
            {
                case "conversational":
                    logger.LogDebug("Creating conversational agent: {AgentName}", name);
                    chatClientUsed = client.GetConversationalClient();
                    agent = chatClientUsed.CreateAIAgent(instructions, name, loggerFactory: factory);
                    logger.LogInformation("Created conversational agent: {AgentName} with {PromptCount} prompts",
                        name, promptsForAgent.Count);
                    break;
                case "vision":
                    logger.LogDebug("Creating vision agent: {AgentName}", name);
                    chatClientUsed = client.GetVisionClient();
                    agent = chatClientUsed.CreateAIAgent(instructions, name, loggerFactory: factory);
                    logger.LogInformation("Created vision agent: {AgentName} with {PromptCount} prompts",
                        name, promptsForAgent.Count);
                    break;
                case "image":
                    logger.LogDebug("Creating image generation agent: {AgentName}", name);
                    imageGenerator = client.GetImageClient();
                    var agentBase = client.GetVisionClient(); // agent doesn't direct support image generator yet
                    agent = agentBase.CreateAIAgent(name: name, loggerFactory: factory);
                    logger.LogInformation("Created image generation agent: {AgentName} with {PromptCount} prompts",
                           name, promptsForAgent.Count);
                    break;
                default:
                    logger.LogError("Unsupported agent type: {AgentType} for agent: {AgentName}", agentType, name);
                    throw new NotSupportedException(
                        $"Agent type '{agentType}' is not supported.");
            }

            var agentName = agent.Name ?? throw new InvalidOperationException("Agent should have a unique name.");

            foreach (var prompt in promptsForAgent)
            {
                logger.LogDebug("Agent {AgentName} prompt registered: {PromptKey}", agentName, prompt.Key);
            }

            return new AgentCacheEntry
            {
                AgentName = agentName,
                Agent = agent,
                ChatClient = chatClientUsed,
                ImageGenerator = imageGenerator,
                Prompts = new Dictionary<string, string>(promptsForAgent)
            };
        }

        public void Parse(string markdown)
        {
            logger.LogInformation("Starting agent markdown parsing");

            if (string.IsNullOrWhiteSpace(markdown))
            {
                logger.LogWarning("Markdown content is empty or null");
                return;
            }

            bool inAgentSection = false;
            bool inPromptSection = false;

            var currentAgent = string.Empty;
            var currentInstructions = string.Empty;
            var currentPrompt = string.Empty;
            var promptsForAgent = new Dictionary<string, string>();
            var agentCount = 0;

            var lines = markdown.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            logger.LogDebug("Parsing {LineCount} lines of markdown", lines.Length);

            foreach (var line in lines)
            {
                if (inAgentSection)
                {
                    if (inPromptSection)
                    {
                        if (line.StartsWith(promptMark))
                        {
                            // Save previous prompt
                            if (!string.IsNullOrEmpty(currentPrompt))
                            {
                                logger.LogDebug("Completed prompt '{PromptKey}' for agent '{AgentName}' ({InstructionLength} chars)",
                                    currentPrompt, currentAgent, currentInstructions.Trim().Length);
                                promptsForAgent[currentPrompt] = currentInstructions.Trim();
                            }

                            // Start new prompt
                            currentPrompt = line[promptMark.Length..].Trim();
                            logger.LogDebug("Starting new prompt '{PromptKey}' for agent '{AgentName}'",
                                currentPrompt, currentAgent);
                            currentInstructions = string.Empty;
                        }
                        else if (line.StartsWith(agentMark))
                        {
                            // Save last prompt of the agent
                            if (!string.IsNullOrEmpty(currentPrompt))
                            {
                                logger.LogDebug("Saving final prompt '{PromptKey}' for agent '{AgentName}'",
                                    currentPrompt, currentAgent);
                                promptsForAgent[currentPrompt] = currentInstructions.Trim();
                            }

                            if (promptsForAgent.Any())
                            {
                                logger.LogInformation("Finalizing agent '{AgentName}' with {PromptCount} prompts",
                                    currentAgent, promptsForAgent.Count);

                                var agentEntry = MakeAgent(currentAgent,
                                    currentInstructions,
                                    new Dictionary<string, string>(promptsForAgent));

                                agentCache.AddAgent(agentEntry);
                                agentCount++;
                                logger.LogInformation("Successfully cached agent '{AgentName}'", agentEntry.AgentName);
                            }

                            currentAgent = line[agentMark.Length..].Trim();
                            currentInstructions = string.Empty;
                            currentPrompt = string.Empty;
                            promptsForAgent.Clear();
                            logger.LogDebug("Starting new agent definition: '{AgentName}'", currentAgent);
                        }
                        else
                        {
                            // Accumulate instructions
                            currentInstructions += line + "\n";
                        }
                    }
                    else
                    {
                        if (line.StartsWith(promptMark))
                        {
                            inPromptSection = true;
                            currentPrompt = line[promptMark.Length..].Trim();
                            logger.LogDebug("Entering prompt section '{PromptKey}' for agent '{AgentName}'",
                                currentPrompt, currentAgent);
                            currentInstructions = string.Empty;
                        }
                        else if (line.StartsWith(agentMark))
                        {
                            // Start new agent
                            currentAgent = line[agentMark.Length..].Trim();
                            currentInstructions = string.Empty;
                            promptsForAgent.Clear();
                            logger.LogDebug("Starting new agent definition: '{AgentName}'", currentAgent);
                        }
                    }
                }
                else if (line.StartsWith(agentMark))
                {
                    inAgentSection = true;
                    inPromptSection = false;
                    currentAgent = line[agentMark.Length..].Trim();
                    logger.LogDebug("Entering agent section: '{AgentName}'", currentAgent);
                }
            }

            // Handle any remaining agent at end of file
            if (inAgentSection && !string.IsNullOrEmpty(currentAgent))
            {
                if (!string.IsNullOrEmpty(currentPrompt))
                {
                    logger.LogDebug("Processing final prompt '{PromptKey}' for agent '{AgentName}' at end of file",
                        currentPrompt, currentAgent);
                    promptsForAgent[currentPrompt] = currentInstructions.Trim();
                }

                if (promptsForAgent.Any())
                {
                    logger.LogInformation("Finalizing last agent '{AgentName}' with {PromptCount} prompts",
                        currentAgent, promptsForAgent.Count);

                    var agentEntry = MakeAgent(currentAgent,
                        currentInstructions,
                        new Dictionary<string, string>(promptsForAgent));

                    agentCache.AddAgent(agentEntry);
                    agentCount++;
                    logger.LogInformation("Successfully cached final agent '{AgentName}'", agentEntry.AgentName);
                }
            }

            logger.LogInformation("Agent markdown parsing complete: {AgentCount} agents created", agentCount);
        }
    }
}

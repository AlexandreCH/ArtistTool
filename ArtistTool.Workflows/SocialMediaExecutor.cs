using ArtistTool.Domain;
using ArtistTool.Domain.Agents;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ArtistTool.Workflows
{
    public class SocialMediaExecutor(string id, string mediumTxt, ClientFactory clientFactory, ILoggerFactory factory) : ReflectingExecutor<SocialMediaExecutor>(id), IMessageHandler<ChatMessage, ChatMessage>
    {
        const string name = "Social media content producer";

        const string instructions = @"Role
You are a Social Media Content Creator specializing in art and photography.
Task
Create engaging social media posts to promote fine-art prints across socical media platforms such as Instagram and Pinterest. Posts should:

Highlight the photograph’s unique qualities and medium benefits.
Use platform-appropriate tone and formatting (e.g., hashtags and emojis for Instagram, concise hooks for Twitter).
Include calls to action (e.g., “Shop now,” “Limited edition available”).
Incorporate visual cues (suggested image style or layout if relevant).";

        const string channelsPrompt = @"Task
List and prioritize social media channels for promoting the given photograph and print medium. For each channel, consider:

Platform name (e.g., Instagram, Facebook, Pinterest, TikTok, Twitter/X, LinkedIn).
Why it fits (audience demographics, engagement style, visual strengths).
Content format recommendations (e.g., carousel, short video, story, static image).
Hashtag or trend considerations (if relevant). Please return this as a list with one entry per channel. Here is the medium and photo information:"; 

        const string perChannelPrompt = @"For the social media channel '{0}', Given the photograph description and the chosen print medium, and focusing on the specified channel, provide:

Channel Overview

Pros and cons of using this channel for art promotion (reach, engagement style, limitations).

Audience Fit

Who you’ll reach here and why this channel suits the photo + medium.

Content Strategy

Tone and format guidelines for this channel (hashtags, image specs, character limits).

Sample Posts

3–5 post examples tailored to the photograph and medium, including hooks, hashtags, and CTAs.

Best Practices

Posting cadence, timing, and engagement tips.";  
        private readonly ILogger logger = factory.CreateLogger<SocialMediaExecutor>();

        private async Task<ChatClientAgent> GetAgentAsync()
        {
            logger.LogDebug("Creating Research Agent with instructions: {Instructions}", instructions);
            var llm = await clientFactory.GetChatClientAsync(ChatClientType.Conversational);
            return new ChatClientAgent(llm, instructions, name, loggerFactory: factory);
        }

        public async ValueTask<ChatMessage> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            List<DataContent> results = [];
            var medium = message.Contents.OfType<DataContent>()
                .First(dc => !string.IsNullOrWhiteSpace(dc.Name) && dc.Name.StartsWith("Medium preview:"));
            logger.LogDebug("Social media marketer has identified medium: {Medium}", mediumTxt);
            var photoType = $"application/json+{typeof(Photograph).Name.ToLowerInvariant()}";
            var photoEntry = message.Contents.OfType<DataContent>()
                .First(dc => !string.IsNullOrWhiteSpace(dc.MediaType) && dc.MediaType.Equals(photoType, StringComparison.OrdinalIgnoreCase));
            var photo = System.Text.Json.JsonSerializer.Deserialize<Photograph>(Encoding.UTF8.GetString(photoEntry.Data.Span))!;
            logger.LogDebug("Social media marketer has received photograph with title: {Title}", photo.Title);
            var extra = @$"{Environment.NewLine}The medium is: '{mediumTxt}'. The photograph title is '{photo.Title}' and description is '{photo.Description}'. The associated tags are: {string.Join('|', photo.Tags)}";

            var agent = await GetAgentAsync();
            var prompt = $"{channelsPrompt}{extra}";
            logger.LogDebug("Social media marketer is sending prompt to agent: {Prompt}", prompt);
            var response = await agent.RunAsync<ChannelListingResponse>(prompt, cancellationToken: cancellationToken);
            logger.LogDebug("Social media marketer received channel recommendations: {Result}", string.Join(", ", response.Result.Channels.Select(c => c.Name)));
            foreach (var channel in response.Result.Channels)
            {
                await context.YieldOutputAsync(channel);
                logger.LogDebug("Analyzing channel {Channel} for medium {Medium} version of {Title}", channel.Name, mediumTxt, photo.Title);
                var promptToUse = $"{string.Format(perChannelPrompt, channel.Name)}{extra}";
                var result = await agent.RunAsync<SocialMediaChannelResponse>(promptToUse, cancellationToken: cancellationToken);
                logger.LogDebug("Received analysis for channel {Channel} for medium {Medium} version of {Title}", channel, mediumTxt, photo.Title);
                result.Result.Medium = mediumTxt;
                await context.YieldOutputAsync(result.Result);
                results.Add(result.Result.AsSerializedChatParameter());
            }

            return new ChatMessage(ChatRole.Assistant, [..message.Contents, .. results]);
        }
    }
}

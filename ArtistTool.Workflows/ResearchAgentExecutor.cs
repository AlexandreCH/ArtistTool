using ArtistTool.Domain;
using ArtistTool.Domain.Agents;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Text;

namespace ArtistTool.Workflows
{
    internal class ResearchAgentExecutor<TResp>(
        string id, string medium, string name, string instructions, string prompt, ClientFactory client, ILoggerFactory factory) : ReflectingExecutor<ResearchAgentExecutor<TResp>>(id),
        IMessageHandler<ChatMessage, ChatMessage> where TResp : IMediumScopedResult
    {
        private ILogger logger = factory.CreateLogger<ResearchAgentExecutor<TResp>>();

        private ChatClientAgent? _agent; 

        private async Task<ChatClientAgent> GetAgentAsync()
        {
            _agent ??= await CreateAgentAsync();
            return _agent;
        }

        private async Task<ChatClientAgent> CreateAgentAsync()
        {
            logger.LogDebug("Creating Research Agent with instructions: {Instructions}", instructions);
            var llm = await client.GetChatClientAsync(ChatClientType.Conversational);
            return new ChatClientAgent(llm, instructions, name, loggerFactory: factory);
        }
        
        public async ValueTask<ChatMessage> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            var mediumPreview = message.Contents.OfType<DataContent>()
                .First(dc => !string.IsNullOrWhiteSpace(dc.Name) && dc.Name.StartsWith("Medium preview:"));
            logger.LogDebug("Resarch assistant has identified medium: {Medium}", medium);
            var photoType = $"application/json+{typeof(Photograph).Name.ToLowerInvariant()}";
            var photoEntry = message.Contents.OfType<DataContent>()
                .First(dc => !string.IsNullOrWhiteSpace(dc.MediaType) && dc.MediaType.Equals(photoType, StringComparison.OrdinalIgnoreCase));
            var photo = System.Text.Json.JsonSerializer.Deserialize<Photograph>(Encoding.UTF8.GetString(photoEntry.Data.Span))!;
            logger.LogDebug("Research assistant has received photograph with title: {Title}", photo.Title);
            var extra = @$"{Environment.NewLine}The medium is: '{medium}'. The photograph title is '{photo.Title}' and description is '{photo.Description}'. The associated tags are: {string.Join('|',photo.Tags)}";
            var promptToUse = $"{prompt}{extra}";
            logger.LogDebug("Sending prompt to Research Agent: {Prompt}", promptToUse);
            var agent = await GetAgentAsync();
            var response = await agent.RunAsync<TResp>(promptToUse, cancellationToken: cancellationToken);
            logger.LogDebug("Research Agent completed execution.");
            response.Result.Medium = medium;
            await context.YieldOutputAsync(response.Result!);
            var newDc = response.Result!.AsSerializedChatParameter();
            return new ChatMessage(ChatRole.Assistant, [newDc, ..message.Contents]);
        }
    }
}

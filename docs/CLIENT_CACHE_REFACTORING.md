# Client Cache Refactoring - Direct Client Access

## Overview

This refactoring simplifies the architecture by working directly with `IChatClient` and `IImageGenerator` instead of creating `AIAgent` wrappers. The parser reads markdown configuration, and a factory creates the actual client instances.

## New Architecture

### 1. ChatClientType Enum

Defines the three types of AI clients:

```csharp
public enum ChatClientType
{
    Conversational,  // Text-based chat
    Vision,          // Multimodal with image support
    Image            // Image generation
}
```

### 2. ClientCacheEntry

Stores client configuration AND actual client instances:

```csharp
public class ClientCacheEntry
{
    public required string ClientName { get; set; }
    public required ChatClientType ClientType { get; set; }
    public string Instructions { get; set; }
    public IDictionary<string, string> Prompts { get; set; }
    public IChatClient? ChatClient { get; set; }          // For Conversational/Vision
    public IImageGenerator? ImageGenerator { get; set; }  // For Image
}
```

**Key Changes from AgentCacheEntry:**
- ? Removed `AIAgent Agent` - work directly with clients instead
- ? Kept `IChatClient` - used directly, no agent wrapper
- ? Kept `IImageGenerator` - used directly, no agent wrapper
- ? Added `ChatClientType` - explicit type information
- ? Added `Instructions` - system-level instructions (currently unused)

### 3. ClientCache

Manages client configurations and instances (replaces AgentCache):

```csharp
public class ClientCache
{
    public ClientCacheEntry this[string clientName] { get; }
    public IChatClient GetChatClient(string clientName);
    public IImageGenerator GetImageGenerator(string clientName);
    public void AddClient(ClientCacheEntry entry);
    public IEnumerable<ClientCacheEntry> GetClientsByType(ChatClientType type);
    public string GetPrompt(string clientName, string promptKey);
}
```

**Improvements:**
- ? Direct client access - `GetChatClient()`, `GetImageGenerator()`
- ? Type-safe filtering by `ChatClientType`
- ? Better logging
- ? No agent wrapper overhead

### 4. ClientMarkdownParser

Parses markdown and populates `ClientCache` (replaces AgentMarkdownParser):

```csharp
public class ClientMarkdownParser
{
    public ClientMarkdownParser(ClientCache clientCache, ILogger<ClientMarkdownParser> logger);
    public void Parse(string markdown);
}
```

**Key Changes:**
- ? No longer depends on `IAIClientProvider` - doesn't create clients
- ? No longer depends on `ILoggerFactory` - doesn't create agents
- ? Only parses markdown and stores configuration
- ? Cleaner separation of concerns

## Migration Path

### Before (Old Code with Agents)

```csharp
// Parser created agents immediately
var parser = new AgentMarkdownParser(agentCache, clientProvider, loggerFactory, logger);
parser.Parse(markdown);

// Agents were already created and cached
var agent = agentCache["Photo Critique Agent"];
var client = agentCache.GetChatClient("Photo Critique Agent");
```

### After (New Code with Direct Clients)

```csharp
// Step 1: Parser stores configuration
var parser = new ClientMarkdownParser(clientCache, logger);
parser.Parse(markdown);

// Step 2: Factory creates client instances
var factory = new ClientFactory(clientCache, aiClientProvider, logger);
factory.InitializeClients();

// Step 3: Use clients directly (no agents!)
var client = clientCache.GetChatClient("Photo Critique Agent");
var prompt = clientCache.GetPrompt("Photo Critique Agent", "Critique");
var response = await client.GetResponseAsync(prompt);
```

## Benefits

### 1. Separation of Concerns

| Concern | Old Location | New Location |
|---------|-------------|-------------|
| Parse markdown | `AgentMarkdownParser` | `ClientMarkdownParser` |
| Store configuration | `AgentCache` | `ClientCache` |
| Create clients | `AgentMarkdownParser` | `ClientFactory` |
| Create agents | `AgentMarkdownParser` | Not needed! |

### 2. Simpler Architecture

No agent wrapper layer - work directly with `IChatClient` and `IImageGenerator`:

```csharp
// Old: Parse ? Create Agents ? Wrap Clients ? Use Agents
parser.Parse(markdown);
var agent = agentCache["Agent"];
var response = await agent.InvokeAsync(prompt); // Through agent wrapper

// New: Parse ? Create Clients ? Use Clients Directly
parser.Parse(markdown);
factory.InitializeClients();
var client = clientCache.GetChatClient("Agent");
var response = await client.GetResponseAsync(prompt); // Direct!
```

### 3. Testability

```csharp
// Easy to test parser without mocking AI clients
[Fact]
public void Parser_ShouldCreateCacheEntry()
{
    var cache = new ClientCache(logger);
    var parser = new ClientMarkdownParser(cache, logger);
    
    parser.Parse("## Vision Photo Critic\n### Critique\nAnalyze photo...");
    
    Assert.Equal(1, cache.Count);
    Assert.Equal(ChatClientType.Vision, cache["Photo Critic"].ClientType);
}
```

### 4. Type Safety

```csharp
// Explicit type instead of inferring from client
var visionClients = clientCache.GetClientsByType(ChatClientType.Vision);
var imageClients = clientCache.GetClientsByType(ChatClientType.Image);
```

## Markdown Format

The markdown format remains the same:

```markdown
## Conversational Marketing Expert

You are an expert at marketing fine art photography.

### Generate Headline

Create a compelling headline for this photograph...

### Generate Copy

Write marketing copy that highlights...

## Vision Photo Critique Agent

You are an expert photography critic.

### Critique

Analyze this photograph focusing on composition, technique...

## Image Canvas Visualizer

### Generate Canvas Preview

Transform the photograph into a canvas preview...
```

## ClientFactory

The `ClientFactory` creates actual client instances from the cached configuration:

```csharp
public class ClientFactory
{
    public ClientFactory(
        ClientCache clientCache,
        IAIClientProvider aiClientProvider,
        ILogger<ClientFactory> logger);
    
    public void InitializeClients();
    public void InitializeClient(string clientName);
}
```

### How It Works

```csharp
// 1. Parser reads markdown and stores config
parser.Parse(markdown);

// 2. Factory creates client instances based on type
factory.InitializeClients();

// Behind the scenes:
foreach (var entry in clientCache.GetAllClientNames())
{
    switch (entry.ClientType)
    {
        case ChatClientType.Conversational:
            entry.ChatClient = aiClientProvider.GetConversationalClient();
            break;
        case ChatClientType.Vision:
            entry.ChatClient = aiClientProvider.GetVisionClient();
            break;
        case ChatClientType.Image:
            entry.ImageGenerator = aiClientProvider.GetImageClient();
            break;
    }
}
```

## Usage in Executors

Direct client access without agents:

```csharp
public class CritiqueExecutor(ClientCache clients)
{
    const string CRITIQUE_CLIENT = "Photo Critique Agent";
    
    public async Task<CritiqueResponse> ExecuteAsync(Photograph photo)
    {
        // Get prompt from cache
        var prompt = clients.GetPrompt(CRITIQUE_CLIENT, "Critique");
        
        // Get client from cache
        var client = clients.GetChatClient(CRITIQUE_CLIENT);
        
        // Use client directly
        var message = new ChatMessage(ChatRole.User, [
            new TextContent(prompt),
            new DataContent(photoBytes, "image/jpeg")
        ]);
        
        var response = await client.GetResponseAsync<CritiqueResponse>(message);
        return response.Result;
    }
}
```

## Files Created/Updated

- ? `ChatClientType.cs` - Enumeration for client types (Conversational, Vision, Image)
- ? `ClientCacheEntry.cs` - Stores config + client instances
- ? `ClientCache.cs` - Manages client cache with direct access methods
- ? `ClientMarkdownParser.cs` - Parses markdown configuration
- ? `ClientFactory.cs` - Creates actual client instances
- ? `Services.cs` - Updated to use new services
- ? `CritiqueExecutor.cs` - Updated to use ClientCache directly
- ? `Program.cs` - Changed `InitializeAgentsAsync()` to `InitializeClientsAsync()`

## Related Documentation

- [Agent Parser Logging](AGENT_PARSER_LOGGING.md)
- [Workflow Services](../ArtistTool.Workflows/README.md)

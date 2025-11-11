# ArtistTool.Workflows

AI-powered workflow services using Microsoft.Agents.AI for photo critique and analysis.

## Overview

This project provides workflow orchestration services that use AI agents to perform tasks like photo critiques. It uses the `Microsoft.Agents.AI.Workflows` framework to create composable, reusable AI workflows.

## Components

### Core Services

- **`AgentCache`** - Caches and manages AI agent instances
- **`AgentMarkdownParser`** - Parses agent definitions from markdown files
- **`CritiqueExecutor`** - Executes photo critique workflows
- **`WorkflowContext`** - Shared context for workflow execution

### Agent Configuration

Agents are defined in `Agents.md` using a simple markdown format:

```markdown
## Vision Photo Critique Agent

Instructions for the agent...

### Critique

Prompt text...
```

## Usage

### 1. Register Services

In your `Program.cs` or startup configuration:

```csharp
using ArtistTool.Workflows;

// Register workflow services
builder.Services.AddWorkflowServices();
```

### 2. Initialize Agents

After building the service provider, initialize agents from the markdown file:

```csharp
var app = builder.Build();

// Initialize agents from Agents.md
await app.Services.InitializeAgentsAsync();

// Or specify a custom path:
// await app.Services.InitializeAgentsAsync("path/to/custom-agents.md");
```

### 3. Use Workflow Executors

Inject and use the workflow executors in your services or controllers:

```csharp
public class PhotoService
{
    private readonly CritiqueExecutor _critiqueExecutor;
    private readonly IPhotoDatabase _photoDatabase;
    
    public PhotoService(
        CritiqueExecutor critiqueExecutor,
        IPhotoDatabase photoDatabase)
    {
        _critiqueExecutor = critiqueExecutor;
        _photoDatabase = photoDatabase;
    }
    
    public async Task<CritiqueResponse> CritiquePhotoAsync(string photoId)
    {
        var photo = await _photoDatabase.GetPhotographWithIdAsync(photoId);
        
        var context = new WorkflowContext
        {
            Photo = photo
        };
        
        // Execute the critique workflow
        var result = await _critiqueExecutor.HandleAsync(
            context, 
            workflowContext: null, // Or provide IWorkflowContext if available
            cancellationToken: CancellationToken.None);
        
        return result.Critique!;
    }
}
```

## Agent Types

The `AgentMarkdownParser` supports different agent types specified in the agent name:

- **`Conversational`** - Uses conversational AI (text-only)
- **`Vision`** - Uses vision AI (multimodal with image support)

Example:
```markdown
## Conversational Marketing Expert
## Vision Photo Critique Agent
```

## Workflow Context

The `WorkflowContext` class carries state through the workflow:

```csharp
public class WorkflowContext
{
    public Photograph? Photo { get; set; }
    public CritiqueResponse? Critique { get; set; }
    public bool RunningCritique { get; set; } = false;
}
```

## Critique Response

The AI returns structured critique data:

```csharp
public class CritiqueResponse
{
    public Critique[] Critiques { get; set; } = [];
}

public class Critique
{
    public string Area { get; set; } = string.Empty;        // e.g., "Composition", "Technique"
    public short Rating { get; set; }                       // 1-10 scale
    public string Praise { get; set; } = string.Empty;
    public string ImprovementSuggestion { get; set; } = string.Empty;
}
```

## Complete Example

```csharp
// In Program.cs
using ArtistTool.Workflows;

var builder = WebApplication.CreateBuilder(args);

// Add workflow services
builder.Services.AddWorkflowServices();

// Add other services...
builder.Services.AddIntelligenceServices(key => builder.Configuration[key] ?? string.Empty);

var app = builder.Build();

// Initialize agents
await app.Services.InitializeAgentsAsync();

app.Run();
```

```csharp
// In a Blazor component or service
@inject CritiqueExecutor CritiqueExecutor
@inject IPhotoDatabase Db

private async Task CritiqueCurrentPhoto()
{
    var context = new WorkflowContext
    {
        Photo = currentPhoto
    };
    
    var result = await CritiqueExecutor.HandleAsync(
        context, 
        null, 
        CancellationToken.None);
    
    // Display critique
    foreach (var critique in result.Critique.Critiques)
    {
        Console.WriteLine($"{critique.Area}: {critique.Rating}/10");
        Console.WriteLine($"Praise: {critique.Praise}");
        Console.WriteLine($"Suggestion: {critique.ImprovementSuggestion}");
    }
}
```

## Dependencies

- **Microsoft.Agents.AI** - Core agent framework
- **Microsoft.Agents.AI.Workflows** - Workflow orchestration
- **ArtistTool.Intelligence** - AI client providers
- **ArtistTool.Domain** - Domain models (Photograph, Category, etc.)

## Agent Configuration File

The `Agents.md` file must be copied to the output directory. This is configured in the `.csproj`:

```xml
<ItemGroup>
  <None Update="Agents.md">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

## Extending with New Workflows

To create a new workflow executor:

1. Create a class that inherits from `ReflectingExecutor<T>` and implements `IMessageHandler<TIn, TOut>`
2. Register it in `Services.AddWorkflowServices()`
3. Define the agent in `Agents.md`

Example:

```csharp
public class MyCustomExecutor : ReflectingExecutor<MyCustomExecutor>,
    IMessageHandler<WorkflowContext, WorkflowContext>
{
    private readonly AgentCache _agents;
    
    public MyCustomExecutor(string id, AgentCache agents) : base(id)
    {
        _agents = agents;
    }
    
    public async ValueTask<WorkflowContext> HandleAsync(
        WorkflowContext message, 
        IWorkflowContext context, 
        CancellationToken cancellationToken = default)
    {
        // Your workflow logic here
        var agent = _agents["My Agent Name"];
        var chatClient = _agents.GetChatClient("My Agent Name");
        
        // Execute AI operations...
        
        return message;
    }
}
```

## Error Handling

The `AgentMarkdownParser` will throw exceptions for:

- Unsupported agent types
- Missing agent names
- Invalid markdown format

The `InitializeAgentsAsync` method will throw `FileNotFoundException` if the Agents.md file is not found.

## Best Practices

1. **Initialize Early**: Call `InitializeAgentsAsync()` during application startup
2. **Cache Agents**: Use the `AgentCache` to retrieve agents efficiently
3. **Structured Prompts**: Define clear, structured prompts in `Agents.md`
4. **Typed Responses**: Use structured response types with `GetResponseAsync<T>()`
5. **Error Handling**: Wrap workflow execution in try-catch blocks

## Troubleshooting

### "Agent not found in cache"
- Ensure `InitializeAgentsAsync()` was called before using executors
- Check that the agent name in code matches the name in `Agents.md`

### "Agents.md not found"
- Verify the file is set to "Copy to Output Directory"
- Check the path passed to `InitializeAgentsAsync()`

### "Agent type not supported"
- Only "Conversational" and "Vision" types are supported
- Check the agent type prefix in `Agents.md`

## Future Enhancements

- [ ] Support for image generation agents
- [ ] Workflow composition and chaining
- [ ] Async agent execution with progress callbacks
- [ ] Agent versioning and A/B testing
- [ ] Workflow telemetry and monitoring

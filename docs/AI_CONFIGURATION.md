# AI and Workflow Configuration Guide

## Overview

ArtistTool supports optional AI features and workflow capabilities that can be enabled or disabled through configuration. This guide explains how to configure and use these features.

## Configuration Options

All AI and workflow options are configured in `appsettings.json` or `appsettings.Development.json`:

```json
{
  "Options": {
    "UseAI": false,
    "MarketingMode": false,
    "UseWorkflows": false
  }
}
```

### Configuration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `UseAI` | boolean | `false` | Enables AI-powered photo analysis, tagging, and enrichment |
| `MarketingMode` | boolean | `false` | Shows the "Market This" button for canvas preview generation |
| `UseWorkflows` | boolean | `false` | Enables AI agent workflows for photo critique and analysis |

## Feature Dependencies

The features have the following dependencies:

- **`MarketingMode`** requires `UseAI = true` to function
- **`UseWorkflows`** requires `UseAI = true` to function
- If `UseAI = false`, both `MarketingMode` and `UseWorkflows` will be disabled regardless of their settings

## Configuration Scenarios

### Scenario 1: Basic Mode (No AI)

```json
{
  "Options": {
    "UseAI": false,
    "MarketingMode": false,
    "UseWorkflows": false
  }
}
```

**What's enabled:**
- Manual photo uploads
- Manual metadata editing (title, description, tags, categories)
- Photo database with manual management

**What's disabled:**
- Automatic photo analysis
- AI-generated titles and descriptions
- AI-suggested tags and categories
- Marketing canvas previews
- Photo critique workflows

### Scenario 2: AI Enabled (No Marketing or Workflows)

```json
{
  "Options": {
    "UseAI": true,
    "MarketingMode": false,
    "UseWorkflows": false
  }
}
```

**What's enabled:**
- Automatic photo analysis with AI
- AI-generated titles and descriptions
- AI-suggested tags and categories
- Vision-based photo understanding

**What's disabled:**
- Marketing canvas preview generation
- Photo critique workflows

### Scenario 3: AI with Marketing

```json
{
  "Options": {
    "UseAI": true,
    "MarketingMode": true,
    "UseWorkflows": false
  }
}
```

**What's enabled:**
- All AI features from Scenario 2
- "Market This" button on photo detail pages
- Marketing canvas preview generation
- AI-generated marketing copy, headlines, and social media text
- Pricing recommendations

**What's disabled:**
- Photo critique workflows

### Scenario 4: Full AI Suite (Recommended for Advanced Users)

```json
{
  "Options": {
    "UseAI": true,
    "MarketingMode": true,
    "UseWorkflows": true
  }
}
```

**What's enabled:**
- All AI features from Scenario 3
- Photo critique workflows with AI agents
- Structured critique responses with ratings and suggestions
- Workflow-based AI operations

## Azure OpenAI Configuration

When `UseAI = true`, you must configure Azure OpenAI settings:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ConversationalDeployment": "gpt-4o",
    "VisionDeployment": "gpt-4o",
    "ImageDeployment": "dall-e-3"
  }
}
```

### Required Models

- **Conversational Model** (e.g., `gpt-4o`): For text generation, tagging, categorization
- **Vision Model** (e.g., `gpt-4o`): For image analysis and description
- **Image Model** (e.g., `dall-e-3`): For canvas preview generation (Marketing mode only)

## Workflow Configuration

When `UseWorkflows = true`, the application will:

1. Load and parse the `Agents.md` file during startup
2. Initialize AI agents defined in the markdown file
3. Make workflow executors available for dependency injection

### Required File

The `Agents.md` file must be present in the application directory. It defines agent configurations:

```markdown
## Vision Photo Critique Agent

You are a photo critique agent...

### Critique

Please provide a detailed critique...
```

## Application Startup Behavior

### Service Registration

```csharp
// AI disabled - basic services only
if (UseAI == false)
{
    services.AddDomainServices(); // Basic photo database
}

// AI enabled
if (UseAI == true)
{
    services.AddIntelligenceServices(); // AI-powered photo database
    
    // Workflows enabled
    if (UseWorkflows == true)
    {
        services.AddWorkflowServices(); // Agent cache, parsers, executors
    }
}
```

### Initialization Order

1. **Service registration** (based on configuration)
2. **Photo database initialization** (always)
3. **Workflow agent initialization** (only if `UseWorkflows = true`)

### Startup Logs

#### With Workflows Enabled

```
info: Initializing PhotoDatabase
info: PhotoDatabase initialized successfully
info: Initializing AI Workflows
info: AI Workflows initialized successfully
```

#### Without Workflows

```
info: Initializing PhotoDatabase
info: PhotoDatabase initialized successfully
```

## Environment-Specific Configuration

### Development

`appsettings.Development.json`:

```json
{
  "Options": {
    "UseAI": true,
    "MarketingMode": true,
    "UseWorkflows": true
  },
  "AzureOpenAI": {
    "Endpoint": "https://dev-openai.openai.azure.com/",
    "ConversationalDeployment": "gpt-4o",
    "VisionDeployment": "gpt-4o",
    "ImageDeployment": "dall-e-3"
  }
}
```

### Production

`appsettings.json`:

```json
{
  "Options": {
    "UseAI": true,
    "MarketingMode": false,
    "UseWorkflows": false
  }
}
```

## Error Handling

### AI Initialization Failures

If AI services fail to initialize (e.g., invalid Azure OpenAI credentials), the application will:
- Log the error
- Continue to run in basic mode
- Photos will be stored without AI enrichment

### Workflow Initialization Failures

If workflow initialization fails (e.g., `Agents.md` not found), the application will:
- Log the error: `Failed to initialize AI Workflows`
- Continue to run with AI features (if enabled)
- Workflow executors will not be available

## Troubleshooting

### "Agents configuration file not found"

**Problem**: `UseWorkflows = true` but `Agents.md` is missing

**Solution**:
1. Verify `Agents.md` exists in the project
2. Check that the file is set to "Copy to Output Directory" in the `.csproj`:
   ```xml
   <ItemGroup>
     <None Update="Agents.md">
       <CopyToOutputDirectory>Always</CopyToOutputDirectory>
     </None>
   </ItemGroup>
   ```

### "Failed to initialize AI Workflows"

**Problem**: Workflow initialization error during startup

**Solution**:
1. Check the logs for the specific error message
2. Verify `Agents.md` syntax is correct
3. Ensure Azure OpenAI configuration is valid
4. Set `UseWorkflows = false` to disable workflows temporarily

### Marketing Button Not Showing

**Problem**: "Market This" button not visible on photo detail pages

**Solution**:
1. Verify `UseAI = true` in configuration
2. Set `MarketingMode = true` in configuration
3. Restart the application
4. Check that `AIOptions` is properly injected in the component

## Performance Considerations

### Resource Usage

| Feature | Startup Impact | Runtime Impact | Token Usage |
|---------|---------------|----------------|-------------|
| Basic Mode | Minimal | Minimal | None |
| AI Enabled | +2-3 seconds | +1-5 seconds per photo | High |
| Marketing | +1 second | +3-10 seconds per request | Very High |
| Workflows | +1-2 seconds | +2-8 seconds per workflow | High |

### Cost Implications

Enabling AI features incurs Azure OpenAI costs:
- **Photo Analysis**: ~$0.01-0.03 per photo
- **Marketing Generation**: ~$0.02-0.05 per photo
- **Workflow Execution**: ~$0.01-0.04 per execution

## Best Practices

1. **Start Simple**: Begin with `UseAI = false` and enable features gradually
2. **Development vs Production**: Use different settings for each environment
3. **Monitor Costs**: Track Azure OpenAI usage when AI features are enabled
4. **Graceful Degradation**: The app continues to work if AI features fail
5. **Testing**: Test each configuration scenario before deploying

## Code Access to Options

### In Services

```csharp
public class MyService
{
    private readonly AIOptions _options;
    
    public MyService(AIOptions options)
    {
        _options = options;
    }
    
    public void DoSomething()
    {
        if (_options.UseWorkflows)
        {
            // Workflow-specific logic
        }
    }
}
```

### In Blazor Components

```razor
@inject AIOptions Options

@if (Options.ShowMarketing)
{
    <button @onclick="NavigateToMarket">Market This</button>
}
```

## Related Documentation

- [Intelligence Services](../ArtistTool.Intelligence/README.md)
- [Workflow Services](../ArtistTool.Workflows/README.md)
- [Service Configuration](../docs/SERVICE_CONFIGURATION.md)

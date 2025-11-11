# Agent Markdown Parser Logging

## Overview

The `AgentMarkdownParser` includes comprehensive structured logging to help monitor and debug agent initialization from the `Agents.md` file.

## Log Levels

### Information Level

These logs provide high-level status about agent parsing:

```
info: Starting agent markdown parsing
info: Created conversational agent: {AgentName} with {PromptCount} prompts
info: Created vision agent: {AgentName} with {PromptCount} prompts
info: Finalizing agent '{AgentName}' with {PromptCount} prompts
info: Successfully cached agent '{AgentName}'
info: Agent markdown parsing complete: {AgentCount} agents created
```

**Example Output:**
```
info: ArtistTool.Workflows.AgentMarkdownParser[0]
      Starting agent markdown parsing
info: ArtistTool.Workflows.AgentMarkdownParser[0]
      Created vision agent: Photo Critique Agent with 1 prompts
info: ArtistTool.Workflows.AgentMarkdownParser[0]
      Successfully cached agent 'Photo Critique Agent'
info: ArtistTool.Workflows.AgentMarkdownParser[0]
      Agent markdown parsing complete: 1 agents created
```

### Debug Level

These logs provide detailed information about the parsing process:

```
debug: Parsing {LineCount} lines of markdown
debug: Entering agent section: '{AgentName}'
debug: Starting new agent definition: '{AgentName}'
debug: Entering prompt section '{PromptKey}' for agent '{AgentName}'
debug: Completed prompt '{PromptKey}' for agent '{AgentName}' ({InstructionLength} chars)
debug: Creating agent from markdown: {AgentName}
debug: Agent type: {AgentType}, Agent name: {AgentName}, Prompt count: {PromptCount}
debug: Creating conversational agent: {AgentName}
debug: Creating vision agent: {AgentName}
debug: Agent {AgentName} prompt registered: {PromptKey}
```

**Example Output:**
```
debug: ArtistTool.Workflows.AgentMarkdownParser[0]
       Parsing 15 lines of markdown
debug: ArtistTool.Workflows.AgentMarkdownParser[0]
       Entering agent section: 'Vision Photo Critique Agent'
debug: ArtistTool.Workflows.AgentMarkdownParser[0]
       Entering prompt section 'Critique' for agent 'Vision Photo Critique Agent'
debug: ArtistTool.Workflows.AgentMarkdownParser[0]
       Completed prompt 'Critique' for agent 'Vision Photo Critique Agent' (243 chars)
debug: ArtistTool.Workflows.AgentMarkdownParser[0]
       Creating agent from markdown: Vision Photo Critique Agent
debug: ArtistTool.Workflows.AgentMarkdownParser[0]
       Agent type: Vision, Agent name: Photo Critique Agent, Prompt count: 1
debug: ArtistTool.Workflows.AgentMarkdownParser[0]
       Creating vision agent: Photo Critique Agent
debug: ArtistTool.Workflows.AgentMarkdownParser[0]
       Agent Photo Critique Agent prompt registered: Critique
```

### Warning Level

These logs indicate potential issues:

```
warning: Markdown content is empty or null
```

### Error Level

These logs indicate failures:

```
error: Unsupported agent type: {AgentType} for agent: {AgentName}
```

## Configuration

### Enable Agent Parser Logging

In `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "ArtistTool.Workflows": "Debug",
      "ArtistTool.Workflows.AgentMarkdownParser": "Debug"
    }
  }
}
```

### Production Logging

For production, use `Information` level to reduce log volume:

```json
{
  "Logging": {
    "LogLevel": {
      "ArtistTool.Workflows": "Information"
    }
  }
}
```

## Troubleshooting with Logs

### No Agents Created

**Log Pattern:**
```
info: Starting agent markdown parsing
warning: Markdown content is empty or null
```

**Problem**: Agents.md file is empty or not loaded

**Solution**: Verify the file exists and has content

---

**Log Pattern:**
```
info: Starting agent markdown parsing
debug: Parsing 50 lines of markdown
info: Agent markdown parsing complete: 0 agents created
```

**Problem**: No agent markers (`##`) found in markdown

**Solution**: Check markdown syntax - agent sections must start with `##`

### Agent Not Found in Cache

**Log Pattern:**
```
debug: Entering agent section: 'My Agent'
debug: Entering prompt section 'MyPrompt' for agent 'My Agent'
debug: Completed prompt 'MyPrompt' for agent 'My Agent' (100 chars)
// No "Successfully cached agent" message
```

**Problem**: Agent was parsed but not cached (no prompts or invalid structure)

**Solution**: Ensure agent has at least one prompt section (`###`)

### Unsupported Agent Type

**Log Pattern:**
```
error: Unsupported agent type: Custom for agent: My Custom Agent
```

**Problem**: Agent type is not "Conversational" or "Vision"

**Solution**: Update agent name to start with supported type:
- `## Conversational My Agent`
- `## Vision My Agent`

### Prompt Instruction Length Issues

**Log Pattern:**
```
debug: Completed prompt 'MyPrompt' for agent 'MyAgent' (0 chars)
```

**Problem**: Prompt has no instructions

**Solution**: Add content after the prompt marker (`###`)

## Complete Example Log Flow

### Successful Parse

```
info: ArtistTool.Workflows.AgentMarkdownParser[0]
      Starting agent markdown parsing

debug: ArtistTool.Workflows.AgentMarkdownParser[0]
       Parsing 15 lines of markdown

debug: ArtistTool.Workflows.AgentMarkdownParser[0]
       Entering agent section: 'Vision Photo Critique Agent'

debug: ArtistTool.Workflows.AgentMarkdownParser[0]
       Starting new agent definition: 'Vision Photo Critique Agent'

debug: ArtistTool.Workflows.AgentMarkdownParser[0]
       Entering prompt section 'Critique' for agent 'Vision Photo Critique Agent'

debug: ArtistTool.Workflows.AgentMarkdownParser[0]
       Completed prompt 'Critique' for agent 'Vision Photo Critique Agent' (243 chars)

info: ArtistTool.Workflows.AgentMarkdownParser[0]
      Finalizing agent 'Vision Photo Critique Agent' with 1 prompts

debug: ArtistTool.Workflows.AgentMarkdownParser[0]
       Creating agent from markdown: Vision Photo Critique Agent

debug: ArtistTool.Workflows.AgentMarkdownParser[0]
       Agent type: Vision, Agent name: Photo Critique Agent, Prompt count: 1

debug: ArtistTool.Workflows.AgentMarkdownParser[0]
       Creating vision agent: Photo Critique Agent

info: ArtistTool.Workflows.AgentMarkdownParser[0]
      Created vision agent: Photo Critique Agent with 1 prompts

debug: ArtistTool.Workflows.AgentMarkdownParser[0]
       Agent Photo Critique Agent prompt registered: Critique

info: ArtistTool.Workflows.AgentMarkdownParser[0]
      Successfully cached agent 'Photo Critique Agent'

info: ArtistTool.Workflows.AgentMarkdownParser[0]
      Agent markdown parsing complete: 1 agents created
```

## Structured Logging Properties

The parser uses structured logging with these properties:

| Property | Description | Example |
|----------|-------------|---------|
| `AgentName` | Name of the agent | `"Photo Critique Agent"` |
| `AgentType` | Type of agent | `"Vision"`, `"Conversational"` |
| `PromptKey` | Name/key of a prompt | `"Critique"` |
| `PromptCount` | Number of prompts for an agent | `1` |
| `LineCount` | Number of lines in markdown | `15` |
| `InstructionLength` | Character count of instructions | `243` |
| `AgentCount` | Total agents created | `1` |

### Querying Structured Logs

With structured logging, you can query logs efficiently:

**Find all agent creations:**
```
ArtistTool.Workflows.AgentMarkdownParser AND "Created" AND AgentName
```

**Find agents with many prompts:**
```
ArtistTool.Workflows.AgentMarkdownParser AND PromptCount > 5
```

**Find parsing errors:**
```
ArtistTool.Workflows.AgentMarkdownParser AND "error"
```

## Integration with Application Insights

When using Azure Application Insights, these logs will be captured with full structured properties:

```kusto
traces
| where customDimensions.SourceContext contains "AgentMarkdownParser"
| project timestamp, message, 
          AgentName = customDimensions.AgentName,
          AgentType = customDimensions.AgentType,
          PromptCount = customDimensions.PromptCount
| order by timestamp desc
```

## Best Practices

1. **Development**: Use `Debug` level for detailed parsing information
2. **Production**: Use `Information` level to track agent initialization
3. **Monitoring**: Alert on `Error` level logs from AgentMarkdownParser
4. **Performance**: Monitor `LineCount` and parsing duration
5. **Validation**: Check that `AgentCount` matches expected agents

## Related Documentation

- [Workflow Services](../ArtistTool.Workflows/README.md)
- [AI Configuration](AI_CONFIGURATION.md)
- [Telemetry Troubleshooting](TELEMETRY_TROUBLESHOOTING.md)

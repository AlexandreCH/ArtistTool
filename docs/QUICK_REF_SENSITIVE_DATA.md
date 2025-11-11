# Quick Reference: AI Telemetry Sensitive Data

## ? What Was Changed

### ServiceDefaults/Extensions.cs

Added `AlwaysOnSampler` to ensure all traces are captured:

```csharp
.WithTracing(tracing =>
{
    tracing.AddSource("Microsoft.Extensions.AI")
        .SetSampler(new AlwaysOnSampler()) // ? NEW: Captures all traces
        // ... rest of configuration
});
```

## ?? What You'll See Now

With these changes, traces in Aspire Dashboard will show:

| Before | After |
|--------|-------|
| ? No prompt text | ? Full prompt: `"Describe this photograph..."` |
| ? No response text | ? Full response: `"This breathtaking photograph..."` |
| ? No system message | ? System instructions visible |
| ? Token counts | ? Token counts (still visible) |
| ? Model name | ? Model name (still visible) |

## ?? How to View

1. **Start Aspire AppHost**
   ```bash
   dotnet run --project ArtistTool.AppHost
   ```

2. **Open Dashboard**
   - Navigate to `http://localhost:15888` (or URL shown in console)

3. **Select Resource**
   - Click on "artisttool" in Resources list

4. **View Traces**
   - Click "Traces" tab
   - Find `chat.completions` spans
   - Click to expand and see attributes

5. **See Sensitive Data**
   - Look for `gen_ai.prompt` attribute
   - Look for `gen_ai.completion` attribute
   - Look for `gen_ai.system` attribute

## ?? Configuration Status

| Component | Setting | Status |
|-----------|---------|--------|
| AzureOpenAIClientProvider | `EnableSensitiveData = true` | ? Already set |
| ServiceDefaults | `AlwaysOnSampler()` | ? Now configured |
| Activity Source | `Microsoft.Extensions.AI` | ? Already registered |

## ?? Security Notice

**Current Configuration: Development Mode**

Sensitive data is now captured in traces. This includes:
- User prompts
- AI responses
- System instructions
- Image data (as references)

**For Production:**

Consider disabling sensitive data:

```csharp
// In AzureOpenAIClientProvider.BuildEnhancedChatClient
options.EnableSensitiveData = builder.Environment.IsDevelopment();
```

## ?? Troubleshooting

### Still Not Seeing Prompts?

**Check 1: Rebuild and restart**
```bash
dotnet build
dotnet run --project ArtistTool.AppHost
```

**Check 2: Trigger an AI operation**
- Upload a photo (if UseAI = true)
- Click "Market This" (if MarketingMode = true)
- Run a critique workflow (if UseWorkflows = true)

**Check 3: Wait for trace to appear**
- Traces may take a few seconds to appear in the dashboard
- Refresh the Traces view

**Check 4: Verify log level**
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.Extensions.AI": "Debug"
    }
  }
}
```

### Trace Appears But No Sensitive Attributes?

This indicates the trace was sampled before attributes were added.

**Solution**: Verify `AlwaysOnSampler` is configured (already done).

## ?? Expected Trace Structure

```
POST /analyze-photo
?? Blazor.render
?  ?? chat.completions [Microsoft.Extensions.AI]
?     Duration: 2.3s
?     Attributes:
?       ? gen_ai.system: "You are an expert art..."
?       ? gen_ai.prompt: "Describe this photograph..."
?       ? gen_ai.completion: "This breathtaking photograph..."
?       ? gen_ai.usage.prompt_tokens: 150
?       ? gen_ai.usage.completion_tokens: 75
?       ? gen_ai.request.model: "gpt-4o"
```

## ?? Related Documentation

- [Full Guide: SENSITIVE_DATA_TELEMETRY.md](SENSITIVE_DATA_TELEMETRY.md)
- [Telemetry Troubleshooting: TELEMETRY_TROUBLESHOOTING.md](TELEMETRY_TROUBLESHOOTING.md)
- [Agent Parser Logging: AGENT_PARSER_LOGGING.md](AGENT_PARSER_LOGGING.md)

# AI Telemetry Troubleshooting Guide

## Issue: Not seeing Microsoft.Extensions.AI telemetry in Aspire Dashboard

### Root Causes Fixed

1. **Syntax Error in Extensions.cs** ?
   - Had: `.AddSource("ArtistTool.Intelligence)}.*")` (extra parenthesis)
   - Fixed to proper source names

2. **Missing Core AI Source** ?
   - **Microsoft.Extensions.AI uses `"Microsoft.Extensions.AI"` as its activity source**
   - This is the source that actually emits the telemetry
   - Custom source names in `UseOpenTelemetry(sourceName:)` don't create separate traces

3. **Source Registration** ?
   - Added `.AddSource("Microsoft.Extensions.AI")` to OpenTelemetry configuration

## Current Configuration

### ServiceDefaults/Extensions.cs
```csharp
.WithTracing(tracing =>
{
    tracing.AddSource(builder.Environment.ApplicationName)
        // THIS IS THE KEY SOURCE for Microsoft.Extensions.AI
        .AddSource("Microsoft.Extensions.AI")
        // These are for any custom spans you might create
        .AddSource("ArtistTool.Intelligence.conversational")
        .AddSource("ArtistTool.Intelligence.vision")
        .AddAspNetCoreInstrumentation(...)
        .AddHttpClientInstrumentation();
})
```

### AzureOpenAIClientProvider.cs
```csharp
builder.UseOpenTelemetry(
    configure: options =>
    {
        options.EnableSensitiveData = true; // Shows prompts/responses
    });
```

## Verification Steps

### 1. Check if AI is Enabled
```bash
# In appsettings.Development.json or environment
Options:UseAI = true
```

### 2. Verify AppHost is Running
```bash
# Start the Aspire AppHost
dotnet run --project ArtistTool.AppHost
```

### 3. Access Aspire Dashboard
- Usually at: `http://localhost:15888` (check console output)
- Or the URL shown when AppHost starts

### 4. Trigger AI Operations
Upload a photo or click "Market This" to generate AI traces

### 5. View Traces in Dashboard

Navigate to:
1. **Resources** ? Click on "artisttool"
2. **Traces** tab
3. Look for traces containing:
   - `Microsoft.Extensions.AI` as the source
   - Operations like `chat.completions`
   - Spans with AI model info

### 6. Check Logs
Look for these log entries:
```
info: Microsoft.Extensions.AI.ChatClient[1]
      Starting chat completion request
```

## What You Should See

### In Traces Tab
```
POST /market/{id}                         [200ms]
  ?? Blazor server render                [150ms]
      ?? chat.completions                [120ms]  ? AI TRACE
          - source: Microsoft.Extensions.AI
          - model: gpt-4o
          - tokens: 225
```

### In Logs Tab
```
info: Microsoft.Extensions.AI.ChatClient
      Starting chat completion request
      Model: gpt-4o
      Messages: 2

debug: Microsoft.Extensions.AI.ChatClient
      Chat completion request completed
      Duration: 1234ms
      Tokens: {InputTokens: 150, OutputTokens: 75}
```

## Still Not Working?

### Check 1: Verify Options:UseAI is true
```csharp
// In Program.cs, check this evaluates to true
if (builder.Configuration["Options:UseAI"]?.ToLower() == "true")
```

### Check 2: Verify PhotoIntelligenceService is called
Add a breakpoint or log in `PhotoIntelligenceService.AnalyzePhotoAsync` or `GenerateCanvasPreviewAsync`

### Check 3: Check Console for Errors
Look for authentication errors with Azure OpenAI:
```
Azure.Identity: DefaultAzureCredential failed to retrieve a token
```

### Check 4: Verify OTLP Configuration
The Aspire AppHost should automatically configure OTLP. Check if env var is set:
```bash
# Should be set automatically by Aspire
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

### Check 5: Restart Everything
```bash
# Stop all processes
# Rebuild
dotnet build

# Start AppHost
dotnet run --project ArtistTool.AppHost
```

### Check 6: Enable Verbose Logging
In `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.Extensions.AI": "Trace",
      "ArtistTool.Intelligence": "Trace",
      "OpenTelemetry": "Debug"
    }
  }
}
```

## Known Activity Source Names

These are the actual source names used by various libraries:

| Library | Activity Source Name |
|---------|---------------------|
| Microsoft.Extensions.AI | `Microsoft.Extensions.AI` |
| ASP.NET Core | `Microsoft.AspNetCore` |
| HttpClient | `System.Net.Http` |
| Azure SDK | `Azure.*` (various) |

## Advanced Debugging

### List All Activity Sources
Add this to your app startup to see all registered sources:

```csharp
// In Program.cs, after building the app
var sources = AppDomain.CurrentDomain.GetAssemblies()
    .SelectMany(a => a.GetTypes())
    .Where(t => t.IsSubclassOf(typeof(System.Diagnostics.ActivitySource)))
    .ToList();

foreach (var source in sources)
{
    Console.WriteLine($"Activity Source: {source.FullName}");
}
```

### Check OpenTelemetry Configuration
```csharp
// Add after builder.AddServiceDefaults()
var otelOptions = builder.Services.BuildServiceProvider()
    .GetService<IOptions<OpenTelemetryOptions>>();
    
Console.WriteLine($"OTLP Endpoint: {builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]}");
```

### Enable OpenTelemetry Internal Logging
Set environment variable:
```bash
OTEL_LOG_LEVEL=debug
```

## Success Indicators

? You're seeing traces in Aspire Dashboard  
? Traces show "Microsoft.Extensions.AI" as source  
? AI operations show token counts and duration  
? Logs show "Starting chat completion request"  
? Parent-child span relationships are correct  

## Additional Resources

- [Microsoft.Extensions.AI Telemetry](https://learn.microsoft.com/dotnet/ai/telemetry)
- [OpenTelemetry .NET Tracing](https://opentelemetry.io/docs/languages/net/instrumentation/#traces)
- [.NET Aspire Dashboard](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/overview)
- [Activity Source Documentation](https://learn.microsoft.com/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs)

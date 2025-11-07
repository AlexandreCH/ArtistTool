# Logging and OpenTelemetry Integration

## Overview

The `AzureOpenAIClientProvider` now includes built-in support for logging and OpenTelemetry using `ChatClientBuilder` and the Microsoft.Extensions.AI extension methods.

## Features Enabled

### 1. **Structured Logging** (`UseLogging`)
- Logs all AI interactions with structured data
- Includes request/response details
- Integrates with ASP.NET Core's logging pipeline
- Respects log levels configured in `appsettings.json`

### 2. **OpenTelemetry Tracing** (`UseOpenTelemetry`)
- Automatic distributed tracing for all AI calls
- Separate trace sources for conversational and vision clients:
  - `ArtistTool.Intelligence.conversational`
  - `ArtistTool.Intelligence.vision`
- Tracks latency, token usage, and error rates
- Integrates with existing OpenTelemetry configuration in `ServiceDefaults`

### 3. **Function Invocation Support** (`UseFunctionInvocation`)
- Enables future tool/function calling capabilities
- Ready for structured outputs and tool use patterns
- No impact if not used

## Implementation

### ChatClientBuilder Pipeline

```csharp
var builder = new ChatClientBuilder(innerClient);

// Add logging
builder.UseLogging(loggerFactory);

// Add OpenTelemetry
builder.UseOpenTelemetry(
    sourceName: $"ArtistTool.Intelligence.{clientName}",
    configure: options =>
    {
        options.EnableSensitiveData = false; // Protect prompts/responses in prod
    });

// Add function invocation
builder.UseFunctionInvocation();

return builder.Build();
```

### Constructor Changes

The `AzureOpenAIClientProvider` now accepts an optional `ILoggerFactory`:

```csharp
public AzureOpenAIClientProvider(
    string endpoint, 
    string conversationalDeployment = "gpt-4o", 
    string visionDeployment = "gpt-4o",
    string imageDeployment = "gpt-image-1",
    ILoggerFactory? loggerFactory = null)
```

### Dependency Injection

`Program.cs` now passes the logger factory:

```csharp
builder.Services.AddSingleton<IAIClientProvider>(sp => new AzureOpenAIClientProvider(
    builder.Configuration["AzureOpenAI:Endpoint"]!,
    builder.Configuration["AzureOpenAI:ConversationalDeployment"]!,
    builder.Configuration["AzureOpenAI:VisionDeployment"]!,
    builder.Configuration["AzureOpenAI:ImageDeployment"]!,
    sp.GetRequiredService<ILoggerFactory>())); // ? Logger factory injected
```

## What You'll See

### In Logs

With logging enabled, you'll see entries like:

```
info: Microsoft.Extensions.AI.ChatClient[1]
      Starting chat completion request
      Model: gpt-4o
      Messages: 2
      
debug: Microsoft.Extensions.AI.ChatClient[2]
      Chat completion request completed
      Duration: 1234ms
      FinishReason: Stop
      Tokens: {InputTokens: 150, OutputTokens: 75, TotalTokens: 225}
```

### In Telemetry

OpenTelemetry will capture:

- **Traces**: Each AI call as a span with:
  - Operation name (e.g., `chat.completions`)
  - Duration
  - Model name
  - Token counts
  - Success/failure status
  
- **Metrics**: (if configured)
  - Request count
  - Request duration
  - Token usage
  - Error rates

### In Azure Application Insights

If you enable Azure Monitor (see below), you'll see:

- Request dependencies for AI calls
- Performance tracking
- Exception correlation
- End-to-end transaction tracing

## Configuration

### Log Levels

Control AI logging verbosity in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Extensions.AI": "Debug",
      "ArtistTool.Intelligence": "Debug"
    }
  }
}
```

### Sensitive Data in Development

For debugging, you can enable sensitive data logging:

```csharp
builder.UseOpenTelemetry(
    sourceName: $"ArtistTool.Intelligence.{clientName}",
    configure: options =>
    {
        // Enable in dev to see actual prompts/responses
        options.EnableSensitiveData = builder.Environment.IsDevelopment();
    });
```

### Azure Monitor Integration

To send telemetry to Azure Application Insights, uncomment in `ServiceDefaults/Extensions.cs`:

```csharp
if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry()
       .UseAzureMonitor();
}
```

Then set the connection string:

```json
{
  "APPLICATIONINSIGHTS_CONNECTION_STRING": "InstrumentationKey=...;IngestionEndpoint=..."
}
```

Or via environment variable:
```bash
APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=...;IngestionEndpoint=..."
```

## Viewing Telemetry

### Local Development with OTLP

1. Run a local OpenTelemetry Collector or Jaeger:
   ```bash
   docker run -d -p 4317:4317 -p 16686:16686 jaegertracing/all-in-one:latest
   ```

2. Set OTLP endpoint:
   ```bash
   OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
   ```

3. View traces at: `http://localhost:16686`

### Azure Application Insights

1. Create Application Insights resource
2. Get connection string from Azure Portal
3. Add to configuration (see above)
4. View telemetry in Azure Portal ? Application Insights ? Transaction Search

### .NET Aspire Dashboard

If using Aspire:
1. The dashboard automatically displays OpenTelemetry data
2. Navigate to the Aspire Dashboard (typically `http://localhost:15888`)
3. View traces, metrics, and logs

## Troubleshooting

### No Logs Appearing

Check:
- Log level in `appsettings.json` (set to `Debug` for detailed logs)
- Logger factory is passed to `AzureOpenAIClientProvider`
- Console/file logging provider is configured

### No Telemetry

Check:
- `ServiceDefaults` is added: `builder.AddServiceDefaults()`
- OTLP endpoint is configured (if using external collector)
- Azure Monitor connection string is set (if using App Insights)

### High Overhead

If performance is impacted:
- Set log level to `Information` or `Warning`
- Disable `EnableSensitiveData` (it's off by default)
- Use sampling in OpenTelemetry:
  ```csharp
  builder.Services.AddOpenTelemetry()
      .WithTracing(tracing => tracing.SetSampler(new TraceIdRatioBasedSampler(0.1))); // 10% sampling
  ```

## Benefits

? **Observability**: Full visibility into AI operations  
? **Debugging**: Detailed logs for troubleshooting  
? **Performance**: Track latency and token usage  
? **Monitoring**: Alert on errors and anomalies  
? **Cost Tracking**: Monitor token consumption  
? **Compliance**: Audit AI usage (when sensitive data disabled)  

## Future Enhancements

- [ ] Add custom metrics for token costs
- [ ] Implement token usage dashboards
- [ ] Add semantic caching with telemetry
- [ ] Track AI-specific business metrics
- [ ] Add retry policies with instrumentation
- [ ] Implement rate limiting with metrics

## References

- [Microsoft.Extensions.AI Documentation](https://learn.microsoft.com/dotnet/ai/overview)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [Azure Monitor OpenTelemetry](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-overview)
- [.NET Aspire Observability](https://learn.microsoft.com/dotnet/aspire/fundamentals/telemetry)

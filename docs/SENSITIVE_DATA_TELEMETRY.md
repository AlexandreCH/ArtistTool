# Enabling Sensitive Data in AI Telemetry

## Overview

To capture AI prompts and responses in OpenTelemetry traces, you need to enable sensitive data at two levels:

1. **AI Client Level** - Enable in `AzureOpenAIClientProvider`
2. **OpenTelemetry Level** - Ensure traces are captured in ServiceDefaults

## Configuration

### 1. AI Client Configuration (Already Configured)

In `ArtistTool.Intelligence/AzureOpenAIClientProvider.cs`:

```csharp
builder.UseOpenTelemetry(
    configure: options =>
    {
        // Enable detailed telemetry including prompts and responses
        options.EnableSensitiveData = true;
    });
```

? **This is already set to `true` in your codebase.**

### 2. OpenTelemetry Sampling (Now Configured)

In `ArtistTool.ServiceDefaults/Extensions.cs`:

```csharp
.WithTracing(tracing =>
{
    tracing.AddSource("Microsoft.Extensions.AI")
        .SetSampler(new AlwaysOnSampler()) // ? Ensures all traces are recorded
        // ... other configuration
});
```

? **This has been added to ensure all AI traces are captured.**

## What Gets Captured

With `EnableSensitiveData = true`, the following are included in traces:

### Captured Attributes

| Attribute | Description | Example |
|-----------|-------------|---------|
| `gen_ai.prompt` | Full prompt text sent to AI | `"Describe this photograph..."` |
| `gen_ai.completion` | Full response from AI | `"This is a stunning landscape..."` |
| `gen_ai.system` | System prompt/instructions | `"You are an expert photographer..."` |
| `gen_ai.usage.prompt_tokens` | Input token count | `150` |
| `gen_ai.usage.completion_tokens` | Output token count | `75` |
| `gen_ai.usage.total_tokens` | Total tokens used | `225` |
| `gen_ai.request.model` | Model name | `"gpt-4o"` |
| `gen_ai.request.temperature` | Temperature setting | `0.7` |

### Example Trace with Sensitive Data

```
Span: chat.completions
  Attributes:
    gen_ai.system: "You are an expert art and photography analyst..."
    gen_ai.prompt: "Describe this photograph in detail: [image data]"
    gen_ai.completion: "This breathtaking photograph captures a serene moment at golden hour..."
    gen_ai.usage.prompt_tokens: 150
    gen_ai.usage.completion_tokens: 75
    gen_ai.usage.total_tokens: 225
    gen_ai.request.model: "gpt-4o"
```

## Security Considerations

### Development vs Production

**Development:**
```csharp
// In AzureOpenAIClientProvider
options.EnableSensitiveData = true; // OK for dev
```

**Production:**
```csharp
// Recommended for production
options.EnableSensitiveData = false; // Protect customer data
```

### Environment-Based Configuration

You can make this configurable based on environment:

```csharp
private static IChatClient BuildEnhancedChatClient(
    IChatClient innerClient, 
    string clientName, 
    ILoggerFactory? loggerFactory,
    bool isDevelopment) // Add environment parameter
{
    var builder = new ChatClientBuilder(innerClient);

    if (loggerFactory is not null)
    {
        builder.UseLogging(loggerFactory);
    }

    builder.UseOpenTelemetry(
        configure: options =>
        {
            // Only enable sensitive data in development
            options.EnableSensitiveData = isDevelopment;
        });

    builder.UseFunctionInvocation();

    return builder.Build();
}
```

Then update the constructor:

```csharp
public AzureOpenAIClientProvider(
    string endpoint, 
    string conversationalDeployment = "gpt-4o", 
    string visionDeployment = "gpt-4o",
    string imageDeployment = "gpt-image-1",
    ILoggerFactory? loggerFactory = null,
    bool isDevelopment = false) // Add parameter
{
    // ... existing code ...
    
    _conversationalClient = BuildEnhancedChatClient(
        conversationalChatClient.AsIChatClient(), 
        "conversational", 
        loggerFactory,
        isDevelopment); // Pass environment
}
```

## Viewing Traces in Aspire Dashboard

With sensitive data enabled, you'll see:

### 1. Navigate to Traces
1. Open Aspire Dashboard (typically `http://localhost:15888`)
2. Click on "ArtistTool" resource
3. Go to "Traces" tab

### 2. Find AI Operations
Look for spans named:
- `chat.completions`
- `image.generation`
- `embedding.generation`

### 3. View Trace Details
Click on a trace to see:
- **Timeline**: Duration of AI operations
- **Attributes**: All captured data including prompts and responses
- **Events**: Key moments during processing

### Example View

```
?? POST /market/{id}
?  ?? Blazor.render
?  ?  ?? chat.completions (Microsoft.Extensions.AI)
?  ?     Attributes:
?  ?       gen_ai.system: "You are an expert art and photography analyst..."
?  ?       gen_ai.prompt: "Describe this photograph in detail..."
?  ?       gen_ai.completion: "This breathtaking photograph captures..."
?  ?       gen_ai.usage.total_tokens: 225
```

## Sampling Strategies

### Always On (Current Configuration)

```csharp
.SetSampler(new AlwaysOnSampler())
```

? **Pros**: Captures all AI interactions
? **Cons**: High data volume, higher costs

### Probability-Based Sampling

For production with high volume:

```csharp
.SetSampler(new TraceIdRatioBasedSampler(0.1)) // 10% of traces
```

? **Pros**: Reduces data volume
? **Cons**: May miss important traces

### Parent-Based Sampling

Inherit sampling decision from parent:

```csharp
.SetSampler(new ParentBasedSampler(new AlwaysOnSampler()))
```

? **Pros**: Consistent with parent trace
? **Cons**: Depends on parent sampling decision

### Custom Sampling for AI

Sample only AI operations:

```csharp
.SetSampler(new AlwaysOnSampler()) // For Microsoft.Extensions.AI source
```

Combined with filters in `AddAspNetCoreInstrumentation` to exclude other traces.

## Troubleshooting

### Issue: Not Seeing Prompts/Responses

**Check 1: EnableSensitiveData**
```csharp
// In AzureOpenAIClientProvider.cs
options.EnableSensitiveData = true; // Must be true
```

**Check 2: Sampler Configuration**
```csharp
// In Extensions.cs
.SetSampler(new AlwaysOnSampler()) // Should be set
```

**Check 3: Source Registration**
```csharp
// In Extensions.cs
.AddSource("Microsoft.Extensions.AI") // Must be present
```

### Issue: Traces Showing But No Attributes

This usually means the sampler is dropping the traces before attributes are captured.

**Solution**: Use `AlwaysOnSampler` or increase sampling ratio.

### Issue: High Data Volume

If you're generating too much telemetry data:

**Solution 1**: Use ratio-based sampling
```csharp
.SetSampler(new TraceIdRatioBasedSampler(0.1))
```

**Solution 2**: Disable in production
```csharp
options.EnableSensitiveData = false; // Production setting
```

**Solution 3**: Filter by operation
```csharp
.AddProcessor(new FilteringProcessor(
    activity => activity.Source.Name == "Microsoft.Extensions.AI"))
```

## Data Privacy and Compliance

### PII Considerations

Prompts and responses may contain:
- ?? User-generated content
- ?? Image data (as base64 or URLs)
- ?? Personally identifiable information (PII)
- ?? Business-sensitive information

### Compliance Requirements

**GDPR/CCPA Considerations:**
- Sensitive data in traces is subject to data protection regulations
- May require data retention policies
- Consider anonymization or pseudonymization
- Document what data is captured

### Recommendations

1. **Development Only**: Enable sensitive data only in dev environments
2. **Access Control**: Restrict who can view traces with sensitive data
3. **Retention Policy**: Set appropriate retention periods for trace data
4. **Audit Trail**: Log who accesses sensitive trace data
5. **Customer Consent**: Ensure users consent to data collection

## Alternative: Logging Without Traces

If you need to see AI interactions without full tracing:

```csharp
builder.UseLogging(loggerFactory); // Logs prompts/responses to logs
```

Then view in logs instead of traces:
```
debug: Microsoft.Extensions.AI.ChatClient[0]
       Prompt: "Describe this photograph..."
       Response: "This breathtaking photograph..."
```

## Configuration Checklist

- [x] `EnableSensitiveData = true` in `AzureOpenAIClientProvider`
- [x] `SetSampler(new AlwaysOnSampler())` in `ServiceDefaults`
- [x] `AddSource("Microsoft.Extensions.AI")` registered
- [ ] Verify environment (dev vs prod)
- [ ] Review data privacy implications
- [ ] Test trace visibility in Aspire Dashboard

## References

- [OpenTelemetry Sampling](https://opentelemetry.io/docs/concepts/sampling/)
- [Microsoft.Extensions.AI Telemetry](https://learn.microsoft.com/dotnet/ai/telemetry)
- [.NET Aspire Telemetry](https://learn.microsoft.com/dotnet/aspire/fundamentals/telemetry)

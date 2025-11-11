# MessageHub Troubleshooting Guide

## Issues Fixed

### Issue 1: GUID Mismatch in Subscribe Method

**Problem:**
```csharp
public string Subscribe<TPayload>(Action<Message<TPayload>> handler)
{
    var id = Guid.NewGuid().ToString();  // First GUID
    if (subscriptions.TryAdd(Guid.NewGuid().ToString(),  // Second GUID - DIFFERENT!
        new SubscriptionInfo(typeof(TPayload), handler)))
    {
        return id;  // Returns first GUID, but second one is the key!
    }
}
```

**Impact:**
- Subscription was stored with one GUID as the key
- A different GUID was returned to the caller
- `Unsubscribe(id)` would fail because the ID didn't match any key
- However, this didn't affect publishing because publish looks up by payload type, not ID

**Fix:**
```csharp
public string Subscribe<TPayload>(Action<Message<TPayload>> handler)
{
    var id = Guid.NewGuid().ToString();
    if (subscriptions.TryAdd(id, // Use same ID as key
        new SubscriptionInfo(typeof(TPayload), handler)))
    {
        return id;
    }
}
```

### Issue 2: Async Handler Not Supported

**Problem:**
```csharp
MessageHub.Subscribe<StartWorkflowRequest<MarketingWorkflow>>(async message =>
{
    await StartMarketingWorkflowAsync(message.Payload.Id);
    // ^^^^^ This 'await' doesn't work because Action<T> is synchronous!
});
```

**Impact:**
- The `async` lambda was converted to `Action<Message<T>>` 
- The compiler wraps it, but the await doesn't actually wait
- The handler returns immediately, potentially causing race conditions
- The Task returned by `StartMarketingWorkflowAsync` was never awaited or observed

**Fix:**
```csharp
MessageHub.Subscribe<StartWorkflowRequest<MarketingWorkflow>>(message =>
{
    // Fire and forget - intentionally don't await
    _ = StartMarketingWorkflowAsync(message.Payload.Id);
});
```

The `_ = ` pattern explicitly indicates "fire and forget" behavior.

### Issue 3: Lack of Error Handling and Logging

**Problem:**
- No logging when no subscribers found
- No error handling if handler throws exception
- Limited diagnostic information

**Fix:**
Added comprehensive logging:
```csharp
- Log subscriber count when publishing
- Warn if no subscribers found
- Log successful delivery per subscription
- Catch and log exceptions from handlers
```

## How MessageHub Works

### Publishing Flow

```
1. Publish<TPayload>(payload, source)
   ?
2. Find all subscriptions where PayloadType == typeof(TPayload)
   ?
3. Create Message<TPayload> wrapping payload
   ?
4. For each matching subscription:
   - Cast handler to Action<Message<TPayload>>
   - Invoke handler with message
```

### Subscription Flow

```
1. Subscribe<TPayload>(handler)
   ?
2. Generate unique subscription ID (GUID)
   ?
3. Store in ConcurrentDictionary:
   - Key: subscription ID
   - Value: SubscriptionInfo (PayloadType, Handler)
   ?
4. Return subscription ID to caller
```

## Usage Patterns

### Basic Subscription

```csharp
var subscriptionId = messageHub.Subscribe<MyPayload>(message =>
{
    Console.WriteLine($"Received: {message.Payload}");
});

// Later...
messageHub.Unsubscribe(subscriptionId);
```

### Fire-and-Forget Async Handler

```csharp
messageHub.Subscribe<MyPayload>(message =>
{
    _ = ProcessAsync(message.Payload); // Don't await
});
```

### Awaited Async Handler (Not Recommended)

If you need to await, wrap in `Task.Run`:

```csharp
messageHub.Subscribe<MyPayload>(message =>
{
    Task.Run(async () =>
    {
        await ProcessAsync(message.Payload);
    });
});
```

### Error-Safe Handler

```csharp
messageHub.Subscribe<MyPayload>(message =>
{
    try
    {
        ProcessPayload(message.Payload);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to process payload");
    }
});
```

## Diagnostic Logs

### Subscription

```
debug: Subscribed to messages of type StartWorkflowRequest`1 with ID abc123...
```

### Publishing with Subscribers

```
debug: Publishing message of type StartWorkflowRequest`1 from source MarketingWorkflow. Found 1 subscribers.
debug: Successfully delivered message to subscription abc123...
```

### Publishing without Subscribers

```
warning: No subscribers found for message type StartWorkflowRequest`1. Total subscriptions: 3
```

### Handler Error

```
error: Error delivering message to subscription abc123...
  Exception: InvalidOperationException: Workflow already running
```

## Configuration for Detailed Logs

In `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "ArtistTool.Services.MessageHub": "Debug"
    }
  }
}
```

## Common Issues

### Issue: No Subscribers Found

**Symptom:**
```
warning: No subscribers found for message type MyPayload
```

**Causes:**
1. Subscription not registered before publishing
2. Type mismatch (e.g., `StartWorkflowRequest<T>` vs `StartWorkflowRequest<U>`)
3. Subscription in hosted service not started yet

**Solution:**
- Ensure `IHostedService.StartAsync` completes before publishing
- Verify generic type parameters match exactly
- Add logging to confirm subscription happens

### Issue: Handler Not Executing

**Symptom:**
- Publish logs show subscribers found
- But handler code never runs

**Causes:**
1. Exception thrown in handler (now logged)
2. Wrong delegate type cast
3. Async handler not properly handled

**Solution:**
- Check error logs for handler exceptions
- Use fire-and-forget pattern for async handlers
- Add try-catch in handler

### Issue: Race Condition

**Symptom:**
- Handler sometimes runs, sometimes doesn't

**Causes:**
1. Publishing before subscription completes
2. Hosted service starts after page loads

**Solution:**
```csharp
// In Program.cs
var app = builder.Build();

// Ensure hosted services start
await app.StartAsync();

// Then run the app
await app.RunAsync();
```

Or add a delay/retry in the publisher:

```csharp
protected override async Task OnInitializedAsync()
{
    await Task.Delay(100); // Give hosted services time to start
    MessageHub.Publish(payload, GetType());
}
```

## Best Practices

### 1. Unsubscribe When Done

```csharp
public class MyComponent : IDisposable
{
    private string? _subscriptionId;
    
    protected override void OnInitialized()
    {
        _subscriptionId = MessageHub.Subscribe<MyPayload>(HandleMessage);
    }
    
    public void Dispose()
    {
        if (_subscriptionId != null)
        {
            MessageHub.Unsubscribe(_subscriptionId);
        }
    }
}
```

### 2. Handle Errors in Handlers

```csharp
MessageHub.Subscribe<MyPayload>(message =>
{
    try
    {
        Process(message.Payload);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Handler failed");
        // Don't let exception bubble up to MessageHub
    }
});
```

### 3. Use Fire-and-Forget for Async

```csharp
MessageHub.Subscribe<MyPayload>(message =>
{
    _ = ProcessAsync(message.Payload);
});
```

The underscore discard (`_`) makes the intent explicit.

### 4. Verify Type Matching

Generic types must match exactly:

```csharp
// Publisher
MessageHub.Publish<StartWorkflowRequest<MarketingWorkflow>>(...);

// Subscriber (must match exactly!)
MessageHub.Subscribe<StartWorkflowRequest<MarketingWorkflow>>(...);
```

## Testing

### Unit Test Example

```csharp
[Fact]
public void Publish_WithSubscriber_InvokesHandler()
{
    // Arrange
    var messageHub = new MessageHub(logger);
    var received = false;
    
    messageHub.Subscribe<string>(msg => received = true);
    
    // Act
    messageHub.Publish("test", typeof(MyTest));
    
    // Assert
    Assert.True(received);
}
```

### Integration Test with Hosted Service

```csharp
[Fact]
public async Task HostedService_SubscribesBeforePublish()
{
    // Arrange
    var host = CreateHost();
    
    // Act - Start hosted services
    await host.StartAsync();
    
    // Act - Publish message
    var messageHub = host.Services.GetRequiredService<IMessageHub>();
    messageHub.Publish(payload, GetType());
    
    // Assert
    await Task.Delay(100); // Give handler time to execute
    // Assert handler executed
}
```

## Related Documentation

- [WorkflowsHost Implementation](../ArtistTool.Workflows/WorkflowsHost.cs)
- [Message and IMessageHub](../ArtistTool.Services/)

# Blazor Component Re-rendering with Cascading Parameters

## The Problem

### Symptom
The `CritiqueComponent` displays "Waiting for the workflow..." and never updates, even though debugging shows the `Step` property is changing correctly.

### Root Cause

**Blazor doesn't automatically re-render child components when cascaded objects' internal properties change.**

Here's what was happening:

1. ? `MarketingWorkflow.razor` updates `_context` on a timer
2. ? `MarketingWorkflow.razor` calls `StateHasChanged()` 
3. ? `MarketingWorkflow.razor` re-renders
4. ? `CritiqueComponent` doesn't re-render because:
   - The **reference** to `Context` (cascading parameter) didn't change
   - Only the **internal properties** of `Context.Critique` changed
   - Blazor doesn't track internal property changes of cascaded objects

### Code Flow

```
MarketingWorkflow.razor
?? Timer fires every 500ms
?? Updates _context = await Mediator.RequestFor(Id, true)
?? Calls StateHasChanged()
?? Re-renders MarketingWorkflow component
    ?? CascadingValue provides _context to children
        ?? CritiqueComponent receives Context (same reference)
            ?? OnParametersSet() is NOT called (parameter didn't change)
            ?? Component does NOT re-render
            ?? UI shows stale data ?
```

## The Solution

### Subscribe to INotifyPropertyChanged

Since both `MarketingWorkflowContext` and `WorkflowNode<T>` implement `INotifyPropertyChanged`, we can subscribe to property change notifications:

```razor
@code {
    [CascadingParameter]
    public MarketingWorkflowContext? Context { get; set; }

    public WorkflowNode<CritiqueResponse>? Step => Context?.Critique;

    private WorkflowNode<CritiqueResponse>? _previousStep;

    protected override void OnParametersSet()
    {
        // Unsubscribe from previous step if it changed
        if (_previousStep is not null && _previousStep != Step)
        {
            _previousStep.PropertyChanged -= OnStepPropertyChanged;
        }

        // Subscribe to the current step's property changes
        if (Step is not null && _previousStep != Step)
        {
            Step.PropertyChanged -= OnStepPropertyChanged;
            Step.PropertyChanged += OnStepPropertyChanged;
            _previousStep = Step;
        }
    }

    private void OnStepPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Re-render when the workflow step properties change
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (Step is not null)
        {
            Step.PropertyChanged -= OnStepPropertyChanged;
        }
    }
}
```

### Key Points

1. **Subscribe in `OnParametersSet()`**: This runs when the cascading parameter is set
2. **Track previous step**: Prevents duplicate subscriptions
3. **Use `InvokeAsync(StateHasChanged)`**: Ensures thread-safe UI updates
4. **Implement `IDisposable`**: Clean up event subscriptions to prevent memory leaks
5. **Unsubscribe carefully**: Remove old subscriptions before adding new ones

## Alternative Solutions

### Option 1: Force Update from Parent (Not Recommended)

```razor
<!-- In MarketingWorkflow.razor -->
<CascadingValue IsFixed="false" Value="_context">
    <CritiqueComponent @ref="_critiqueRef" />
</CascadingValue>

@code {
    private CritiqueComponent? _critiqueRef;
    
    private async Task UpdateContext()
    {
        _context = await Mediator.RequestFor(Id, true);
        StateHasChanged();
        _critiqueRef?.StateHasChanged(); // Manually trigger child re-render
    }
}
```

? **Problems:**
- Requires parent to know about child components
- Breaks encapsulation
- `StateHasChanged()` is protected, so would need to expose publicly

### Option 2: New Context Instance Each Time (Memory Intensive)

```csharp
// In Mediator
public async Task<MarketingWorkflowContext> RequestFor(string id, bool refresh)
{
    // Always return a NEW instance
    return new MarketingWorkflowContext
    {
        Photo = photo,
        Critique = critique // Clone or create new
    };
}
```

? **Pros:**
- Simple - parameter reference changes trigger re-render
- No need for INotifyPropertyChanged subscriptions

? **Cons:**
- Creates new objects on every poll
- More memory allocations
- Loses in-memory state

### Option 3: Use State Container Pattern

```csharp
public class WorkflowStateContainer
{
    public MarketingWorkflowContext? Context { get; private set; }
    
    public event Action? OnChange;
    
    public void UpdateContext(MarketingWorkflowContext context)
    {
        Context = context;
        OnChange?.Invoke();
    }
}

// Register in DI
services.AddScoped<WorkflowStateContainer>();
```

```razor
@inject WorkflowStateContainer State
@implements IDisposable

@code {
    protected override void OnInitialized()
    {
        State.OnChange += StateHasChanged;
    }
    
    public void Dispose()
    {
        State.OnChange -= StateHasChanged;
    }
}
```

? **Pros:**
- Centralized state management
- Works well with multiple components

? **Cons:**
- More infrastructure code
- Need to manage DI lifetimes

## Best Practices

### 1. Always Implement IDisposable When Subscribing to Events

```razor
@implements IDisposable

@code {
    protected override void OnParametersSet()
    {
        SomeObject.PropertyChanged += Handler;
    }
    
    public void Dispose()
    {
        SomeObject.PropertyChanged -= Handler;
    }
}
```

### 2. Use InvokeAsync for Thread Safety

```csharp
private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    // ? Good - Thread-safe
    InvokeAsync(StateHasChanged);
    
    // ? Bad - May cause threading issues
    StateHasChanged();
}
```

### 3. Check for Null Before Unsubscribing

```csharp
public void Dispose()
{
    if (Step is not null)
    {
        Step.PropertyChanged -= OnStepPropertyChanged;
    }
}
```

### 4. Prevent Duplicate Subscriptions

```csharp
Step.PropertyChanged -= OnStepPropertyChanged; // Remove if exists
Step.PropertyChanged += OnStepPropertyChanged; // Add once
```

## Debugging Tips

### Check if OnParametersSet is Called

```csharp
protected override void OnParametersSet()
{
    Console.WriteLine($"OnParametersSet called. Context: {Context?.Id}");
    // ... rest of code
}
```

### Verify Event Subscription

```csharp
protected override void OnParametersSet()
{
    if (Step is not null)
    {
        var hasHandlers = Step.PropertyChanged?.GetInvocationList().Length ?? 0;
        Console.WriteLine($"Step has {hasHandlers} PropertyChanged handlers");
    }
}
```

### Log When Re-render is Triggered

```csharp
private void OnStepPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    Console.WriteLine($"Step property changed: {e.PropertyName}");
    InvokeAsync(StateHasChanged);
}
```

### Check Cascade Value Updates

```razor
<CascadingValue IsFixed="false" Value="_context">
    <!-- IsFixed="false" means it CAN update -->
    <CritiqueComponent />
</CascadingValue>
```

## Common Mistakes

### Mistake 1: Not Unsubscribing

```csharp
// ? Memory leak - never unsubscribes
protected override void OnParametersSet()
{
    Step.PropertyChanged += OnStepPropertyChanged;
}
```

**Fix:** Implement `IDisposable`

### Mistake 2: Subscribing Multiple Times

```csharp
// ? Subscribes every time OnParametersSet runs
protected override void OnParametersSet()
{
    Step.PropertyChanged += OnStepPropertyChanged; // Duplicate subscriptions!
}
```

**Fix:** Track previous step and unsubscribe first

### Mistake 3: Not Using InvokeAsync

```csharp
// ? May cause threading issues
private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    StateHasChanged(); // Called from timer thread!
}
```

**Fix:** Wrap in `InvokeAsync()`

### Mistake 4: Assuming CascadingValue Triggers Re-render

```razor
<!-- ? Child won't re-render when _context properties change -->
<CascadingValue Value="_context">
    <CritiqueComponent />
</CascadingValue>
```

**Fix:** Subscribe to property changes in child component

## Testing

### Unit Test for Property Change Subscription

```csharp
[Fact]
public void Component_ReRenders_WhenStepPropertyChanges()
{
    // Arrange
    var ctx = new TestContext();
    var workflowContext = new MarketingWorkflowContext();
    var component = ctx.RenderComponent<CritiqueComponent>(parameters =>
        parameters.Add(p => p.Context, workflowContext));
    
    // Act
    workflowContext.Critique.Started = true;
    
    // Assert
    component.WaitForAssertion(() =>
        component.Find(".loading-container").TextContent
            .Should().Contain("generating your critique"));
}
```

## Performance Considerations

### Subscription Overhead

Each property change triggers:
1. Event invocation
2. `InvokeAsync()` call
3. Component re-render
4. Razor template evaluation

**Optimization:** Debounce rapid changes if needed:

```csharp
private System.Threading.Timer? _debounceTimer;

private void OnStepPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    _debounceTimer?.Dispose();
    _debounceTimer = new Timer(_ => InvokeAsync(StateHasChanged), null, 100, Timeout.Infinite);
}
```

## Related Documentation

- [Blazor State Management](https://learn.microsoft.com/aspnet/core/blazor/state-management)
- [Cascading Values and Parameters](https://learn.microsoft.com/aspnet/core/blazor/components/cascading-values-and-parameters)
- [Component Lifecycle](https://learn.microsoft.com/aspnet/core/blazor/components/lifecycle)

using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ArtistTool.Workflows
{
    public class MediumPreviewAggregator :
        AggregatingExecutor<DataContent, DataContent[]>,
            IMessageHandler<DataContent[], DataContent[]>
    {
        private static ILogger? _staticLogger;
        private int expected = 0; 

        public MediumPreviewAggregator(string id,
            int count, ILoggerFactory factory)
            : base($"{id}", Aggregate)
        {
            _staticLogger ??= factory.CreateLogger<MediumPreviewAggregator>();
            expected = count;
        }

        public static DataContent[] Aggregate(DataContent[]? list, DataContent item)
        {
            _staticLogger?.LogDebug("Aggregating item. Current count: {CurrentCount}", list?.Length);
            return [item, .. list ?? []];
        }

        public ValueTask<DataContent[]> HandleAsync(DataContent[] message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            _staticLogger?.LogDebug("Handling aggregated message with {ItemCount} items.", message.Length);
            return ValueTask.FromResult(message);
        }
    }
}
    
using System.Threading.Channels;

namespace ArtistTool.Services
{
    public class MessageHub : IMessageHub
    {
        private readonly Dictionary<Type, object> _channels = [];

        private readonly Lock _mutex = new();

        private Channel<T> CheckChannel<T>()
        {
            lock (_mutex)
            {
                if (_channels.TryGetValue(typeof(T), out var existingChannelObj))
                {
                    return existingChannelObj as Channel<T> ?? throw new InvalidOperationException("Channel retrieval failed.");
                }
                else
                {
                    var channel = Channel.CreateUnbounded<T>();
                    _channels[typeof(T)] = channel;
                    return channel;
                }
            }
        }

        public Action Subscribe<T>(Action<T> handler)
        {
            Channel<T> channel = CheckChannel<T>() ?? throw new InvalidOperationException("Channel creation failed.");

            var reader = channel.Reader;
            var writer = channel.Writer;

            var taskCancellation = new CancellationTokenSource();

            var token = taskCancellation.Token;

            var listener = Task.Run(async () =>
            {
                await foreach (var message in reader.ReadAllAsync(token))
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                    handler(message);
                }
            });

            void unsubscribe() => taskCancellation.Cancel();
            
            return unsubscribe;
        }

        public void Publish<T>(T message)
        {
            Channel<T> channel = CheckChannel<T>() ?? throw new InvalidOperationException("Channel creation failed.");
            var writer = channel.Writer;
            writer.TryWrite(message);

        }
    }
}

using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Administrator.Core;

public delegate Task BatchHandler<TEventArgs>(ICollection<TEventArgs> eventArgs, CancellationToken cancellationToken) where TEventArgs : EventArgs;

public class BatchEventDispatcher<TKey, TEventArgs>(BatchHandler<TEventArgs> batchHandler, int maxBatchSize = 5, TimeSpan? rateLimitInterval = null)
    where TKey : notnull
    where TEventArgs : EventArgs
{
    private readonly ConcurrentDictionary<TKey, Channel<TEventArgs>> _channels = new();
    private readonly TimeSpan _rateLimitInterval = rateLimitInterval ?? TimeSpan.FromSeconds(1);

    public ValueTask WriteAsync(TKey key, TEventArgs item, CancellationToken cancellationToken = default)
    {
        var channel = GetChannel(key, cancellationToken);
        return channel.Writer.WriteAsync(item, cancellationToken);
    }
    
    private Channel<TEventArgs> GetChannel(TKey key, CancellationToken cancellationToken)
    {
        var channel = _channels.GetOrAdd(key, static (k, state) =>
        {
            var channel = Channel.CreateUnbounded<TEventArgs>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            _ = LoopIndividualAsync(channel, state);
            
            return channel;
        }, new State(maxBatchSize, batchHandler, _rateLimitInterval, cancellationToken));

        return channel;
    }

    private static async Task LoopIndividualAsync(Channel<TEventArgs> channel, State state)
    {
        while (!state.CancellationToken.IsCancellationRequested && await channel.Reader.WaitToReadAsync(state.CancellationToken))
        {
            var delay = Task.Delay(state.RateLimitInterval, state.CancellationToken);
            List<TEventArgs> batch = [];
            while (batch.Count < state.MaxBatchSize && channel.Reader.TryRead(out var item))
            {
                batch.Add(item);
            }

            try
            {
                await state.BatchHandler.Invoke(batch, state.CancellationToken);
            }
            catch
            {
                // TODO: We don't have any way to log exceptions. I'm hoping that I will remember to appropriately try/catch in the batch handler.
            }
            
            await delay;
        }
    }

    private sealed record State(int MaxBatchSize, BatchHandler<TEventArgs> BatchHandler, TimeSpan RateLimitInterval, CancellationToken CancellationToken);
}
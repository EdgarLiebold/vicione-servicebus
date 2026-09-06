using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>
/// Provides bounded per-key mutual exclusion using a fixed number of partitions. It intentionally makes no FIFO or
/// global ordering guarantee. Hash collisions may serialize unrelated keys, which is a throughput cost but never a
/// correctness violation.
/// </summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
public sealed class PartitionedConsumerConcurrencyGate<TMessage, TKey> : IConsumerConcurrencyGate<TMessage>, IDisposable
    where TKey : notnull
{
    private readonly IEqualityComparer<TKey> _comparer;
    private readonly SemaphoreSlim[] _partitions;
    private readonly ConsumerPartitionKeySelector<TMessage, TKey> _selector;
    private int _disposed;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="partitionCount">The partition count.</param>
    /// <param name="selector">The selector.</param>
    /// <param name="comparer">The comparer.</param>
    public PartitionedConsumerConcurrencyGate(
        int partitionCount,
        ConsumerPartitionKeySelector<TMessage, TKey> selector,
        IEqualityComparer<TKey>? comparer = null)
    {
        if (partitionCount is < 1 or > ConsumerConcurrencyPolicy.AbsoluteMaximumConcurrency)
        {
            throw new ArgumentOutOfRangeException(
                nameof(partitionCount),
                partitionCount,
                $"Partition count must be between 1 and {ConsumerConcurrencyPolicy.AbsoluteMaximumConcurrency}.");
        }

        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _comparer = comparer ?? EqualityComparer<TKey>.Default;
        _partitions = new SemaphoreSlim[partitionCount];
        for (int index = 0; index < _partitions.Length; index++)
            _partitions[index] = new SemaphoreSlim(1, 1);
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="TState">The state carried by the operation.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="state">The state.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask ExecuteAsync<TState>(
        TMessage message,
        TState state,
        Func<TState, CancellationToken, ValueTask> next,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(next);
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        TKey key = _selector(message) ?? throw new InvalidOperationException("A consumer partition key must not be null.");
        int partitionIndex = (int)((uint)_comparer.GetHashCode(key) % (uint)_partitions.Length);
        return ConsumerConcurrencyGate<TMessage>.ExecuteCoreAsync(
            _partitions[partitionIndex],
            state,
            next,
            cancellationToken);
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        // See ConsumerConcurrencyGate: do not race SemaphoreSlim.Dispose against active or waiting invocations.
        Interlocked.Exchange(ref _disposed, 1);
    }
}

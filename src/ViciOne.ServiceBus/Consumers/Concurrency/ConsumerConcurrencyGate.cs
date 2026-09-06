using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Bounded consumer-local gate for serial or fixed parallel execution.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public sealed class ConsumerConcurrencyGate<TMessage> : IConsumerConcurrencyGate<TMessage>, IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private int _disposed;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="policy">The policy.</param>
    public ConsumerConcurrencyGate(ConsumerConcurrencyPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (policy.Mode == ConsumerConcurrencyMode.Partitioned)
        {
            throw new ArgumentException(
                "Partitioned concurrency requires a strongly typed partition-key selector.",
                nameof(policy));
        }

        int concurrency = policy.Mode == ConsumerConcurrencyMode.Serial ? 1 : policy.Concurrency;
        _semaphore = new SemaphoreSlim(concurrency, concurrency);
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
        return ExecuteCoreAsync(_semaphore, state, next, cancellationToken);
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        // SemaphoreSlim.Dispose is unsafe while an invocation is waiting or active. These gates never access
        // AvailableWaitHandle, so no disposable OS handle is created. Close admission and let accepted work drain.
        Interlocked.Exchange(ref _disposed, 1);
    }

    internal static async ValueTask ExecuteCoreAsync<TState>(
        SemaphoreSlim semaphore,
        TState state,
        Func<TState, CancellationToken, ValueTask> next,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await next(state, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }
}

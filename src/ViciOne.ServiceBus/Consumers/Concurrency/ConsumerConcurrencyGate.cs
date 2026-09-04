using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>
/// Bounded consumer-local gate for serial or fixed parallel execution.
/// </summary>
public sealed class ConsumerConcurrencyGate<TMessage> : IConsumerConcurrencyGate<TMessage>, IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private int _disposed;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="policy">The policy value.</param>
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

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <typeparam name="TState">The t state type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="state">The state value.</param>
    /// <param name="next">The next value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
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

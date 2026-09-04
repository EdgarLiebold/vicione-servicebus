using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a partition implementation.
/// </summary>
public class Partition :
    IAsyncDisposable
{
    readonly int _index;
    readonly SemaphoreSlim _limit;
    long _attemptCount;
    long _failureCount;
    long _successCount;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="index">The index value.</param>
    public Partition(int index)
    {
        _index = index;
        _limit = new SemaphoreSlim(1);
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await _limit.WaitAsync().ConfigureAwait(false);

        _limit.Dispose();
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var partitionScope = context.CreateScope($"partition-{_index}");
        partitionScope.Set(new
        {
            AttemptCount = _attemptCount,
            SuccessCount = _successCount,
            FailureCount = _failureCount
        });
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync<T>(T context, IPipe<T> next, CancellationToken cancellationToken = default)
        where T : class, PipeContext
    {
        cancellationToken.ThrowIfCancellationRequested(); await _limit.WaitAsync(context.CancellationToken).ConfigureAwait(false);

        try
        {
            Interlocked.Increment(ref _attemptCount);

            await next.SendAsync(context).ConfigureAwait(false);

            Interlocked.Increment(ref _successCount);
        }
        catch
        {
            Interlocked.Increment(ref _failureCount);
            throw;
        }
        finally
        {
            _limit.Release();
        }
    }
}

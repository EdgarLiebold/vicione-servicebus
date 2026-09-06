using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// A concurrency limiter (using a semaphore) which can be shared, and adjusted using a management
/// endpoint.
/// </summary>
public class ConcurrencyLimiter :
    IConcurrencyLimiter
{
    readonly string? _id = null!;
    readonly SemaphoreSlim _limit;
    int _concurrencyLimit;
    DateTimeOffset _lastUpdated;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="concurrencyLimit">The concurrency limit.</param>
    /// <param name="id">The id.</param>
    public ConcurrencyLimiter(int concurrencyLimit, string? id = null)
    {
        _concurrencyLimit = concurrencyLimit;
        _id = id;

        _limit = new SemaphoreSlim(concurrencyLimit);
        _lastUpdated = DateTimeOffset.MinValue;
    }

    int IConcurrencyLimiter.Available => _limit.CurrentCount;
    int IConcurrencyLimiter.Limit => _concurrencyLimit;

    /// <summary>Waits for the configured condition.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task WaitAsync(CancellationToken cancellationToken)
    {
        return _limit.WaitAsync(cancellationToken);
    }

    /// <summary>Releases the owned resource.</summary>
    public void Release()
    {
        _limit.Release();
    }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ConsumeAsync(ConsumeContext<SetConcurrencyLimit> context)
    {
        if (_id == null || _id.Equals(context.Message.Id, StringComparison.OrdinalIgnoreCase))
        {
            if (context.Message.Timestamp >= _lastUpdated)
            {
                try
                {
                    var concurrencyLimit = context.Message.ConcurrencyLimit;
                    if (concurrencyLimit < 1)
                        throw new ArgumentOutOfRangeException(nameof(concurrencyLimit), "The concurrency limit must be >= 1");

                    var previousLimit = _concurrencyLimit;
                    if (concurrencyLimit > previousLimit)
                    {
                        var releaseCount = concurrencyLimit - previousLimit;

                        _limit.Release(releaseCount);

                        Interlocked.Add(ref _concurrencyLimit, releaseCount);

                        _lastUpdated = context.Message.Timestamp ?? context.SentTime ?? context.GetTimeProvider().GetUtcNow();
                    }
                    else if (concurrencyLimit < previousLimit)
                    {
                        for (; previousLimit > concurrencyLimit; previousLimit--)
                        {
                            await _limit.WaitAsync().ConfigureAwait(false);

                            Interlocked.Decrement(ref _concurrencyLimit);

                            _lastUpdated = context.Message.Timestamp ?? context.SentTime ?? context.GetTimeProvider().GetUtcNow();
                        }
                    }

                    await context.Advanced().RespondAsync<ConcurrencyLimitUpdated>(new
                    {
                        Timestamp = context.GetTimeProvider().GetUtcNow(),
                        context.Message.Id,
                        context.Message.ConcurrencyLimit
                    }).ConfigureAwait(false);

                    LogContext.Debug?.Log("Set Consumer Limit: {ConcurrencyLimit} ({CommandId})", context.Message.ConcurrencyLimit, context.Message.Id);
                }
                catch (Exception exception)
                {
                    LogContext.Error?.Log(exception, "Set Consumer Limit failed: {ConcurrencyLimit} ({CommandId})", context.Message.ConcurrencyLimit,
                        context.Message.Id);

                    throw;
                }
            }
            else
                throw new CommandException("The concurrency limit was updated after the command was sent.");
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.Middleware.ConcurrencyLimiting;

/// <summary>
/// Shares one concurrency budget across consumer pipelines and applies timestamp-ordered management commands.
/// </summary>
internal sealed class ConcurrencyLimiter :
    IConcurrencyLimiter
{
    readonly SemaphoreSlim _adjustment;
    readonly string? _id;
    readonly SemaphoreSlim _limit;
    int _concurrencyLimit;
    DateTimeOffset _lastUpdated;

    /// <summary>Creates a shared limiter with an optional command-routing identifier.</summary>
    /// <param name="concurrencyLimit">The positive maximum number of concurrent operations.</param>
    /// <param name="limiterId">The optional identifier that targeted management commands must match.</param>
    public ConcurrencyLimiter(int concurrencyLimit, string? limiterId = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(concurrencyLimit, 1);
        if (limiterId != null)
            ArgumentException.ThrowIfNullOrWhiteSpace(limiterId);

        _concurrencyLimit = concurrencyLimit;
        _id = limiterId;

        _adjustment = new SemaphoreSlim(1, 1);
        _limit = new SemaphoreSlim(concurrencyLimit);
        _lastUpdated = DateTimeOffset.MinValue;
    }

    int IConcurrencyLimiter.Available => _limit.CurrentCount;
    int IConcurrencyLimiter.Limit => Volatile.Read(ref _concurrencyLimit);

    /// <summary>Waits until one concurrency permit is available.</summary>
    /// <param name="cancellationToken">The token that cancels the wait.</param>
    /// <returns>A task that completes after acquiring one permit.</returns>
    public Task WaitAsync(CancellationToken cancellationToken)
    {
        return _limit.WaitAsync(cancellationToken);
    }

    /// <summary>Returns one previously acquired concurrency permit.</summary>
    public void Release()
    {
        _limit.Release();
    }

    /// <summary>Applies a matching non-stale limit command and responds with the committed limit.</summary>
    /// <param name="context">The management command, response endpoint, clock, and cancellation boundary.</param>
    /// <returns>A task that completes after the adjustment and response.</returns>
    public async Task ConsumeAsync(ConsumeContext<SetConcurrencyLimit> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        SetConcurrencyLimit command = context.Message;
        ArgumentNullException.ThrowIfNull(command);
        if (command.LimiterId != null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(command.LimiterId);
            if (!string.Equals(_id, command.LimiterId, StringComparison.OrdinalIgnoreCase))
                return;
        }

        int concurrencyLimit = command.ConcurrencyLimit;
        if (concurrencyLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(command.ConcurrencyLimit), concurrencyLimit, "The concurrency limit must be at least one.");

        DateTimeOffset commandTimestamp = command.Timestamp
            ?? context.SentTime
            ?? context.GetTimeProvider().GetUtcNow();

        try
        {
            await _adjustment.WaitAsync(context.CancellationToken).ConfigureAwait(false);
            try
            {
                if (commandTimestamp < _lastUpdated)
                    throw new CommandException("The concurrency limit was updated after the command was sent.");

                int previousLimit = Volatile.Read(ref _concurrencyLimit);
                if (concurrencyLimit > previousLimit)
                    _limit.Release(concurrencyLimit - previousLimit);
                else if (concurrencyLimit < previousLimit)
                    await TakePermitsAsync(previousLimit - concurrencyLimit, context.CancellationToken).ConfigureAwait(false);

                Volatile.Write(ref _concurrencyLimit, concurrencyLimit);
                _lastUpdated = commandTimestamp;
            }
            finally
            {
                _adjustment.Release();
            }

            await context.Advanced().RespondAsync<ConcurrencyLimitUpdated>(new
            {
                Timestamp = context.GetTimeProvider().GetUtcNow(),
                LimiterId = _id,
                command.ConcurrencyLimit
            }).ConfigureAwait(false);

            LogContext.Debug?.Log("Set Consumer Limit: {ConcurrencyLimit} ({LimiterId})", command.ConcurrencyLimit, _id);
        }
        catch (Exception exception)
        {
            LogContext.Error?.Log(exception, "Set Consumer Limit failed: {ConcurrencyLimit} ({LimiterId})", command.ConcurrencyLimit, _id);

            throw;
        }
    }

    async Task TakePermitsAsync(int count, CancellationToken cancellationToken)
    {
        var acquired = 0;
        try
        {
            for (; acquired < count; acquired++)
                await _limit.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            if (acquired > 0)
                _limit.Release(acquired);

            throw;
        }
    }
}

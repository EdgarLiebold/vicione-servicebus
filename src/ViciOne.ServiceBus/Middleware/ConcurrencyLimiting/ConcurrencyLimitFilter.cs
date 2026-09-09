using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.Middleware.ConcurrencyLimiting;

/// <summary>
/// Bounds the number of operations executing in the downstream pipeline and supports serialized limit adjustments.
/// </summary>
/// <typeparam name="TContext">The context type carried by the pipeline.</typeparam>
internal sealed class ConcurrencyLimitFilter<TContext> :
    IFilter<TContext>,
    IPipe<CommandContext<SetConcurrencyLimit>>
    where TContext : class, PipeContext
{
    readonly SemaphoreSlim _adjustment;
    readonly SemaphoreSlim _limit;
    int _concurrencyLimit;
    DateTimeOffset _lastUpdated;

    /// <summary>Creates a filter with the specified positive concurrency limit.</summary>
    /// <param name="concurrencyLimit">The maximum number of concurrent downstream operations.</param>
    public ConcurrencyLimitFilter(int concurrencyLimit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(concurrencyLimit, 1);

        _concurrencyLimit = concurrencyLimit;

        _adjustment = new SemaphoreSlim(1, 1);
        _limit = new SemaphoreSlim(concurrencyLimit);
        _lastUpdated = DateTimeOffset.MinValue;
    }

    /// <summary>Reports the configured limit and the number of immediately available permits.</summary>
    /// <param name="context">The probe that receives the limiter state.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateFilterScope("concurrencyLimit");
        scope.Add("limit", Volatile.Read(ref _concurrencyLimit));
        scope.Add("available", _limit.CurrentCount);
    }

    /// <summary>Waits for a permit, invokes the downstream stage, and releases the permit on every exit path.</summary>
    /// <param name="context">The pipeline context whose cancellation stops admission.</param>
    /// <param name="next">The downstream pipeline stage.</param>
    /// <returns>A task that completes with the downstream stage.</returns>
    [DebuggerNonUserCode]
    public async Task SendAsync(TContext context, IPipe<TContext> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var waitAsyncTask = _limit.WaitAsync(context.CancellationToken);
        if (waitAsyncTask.Status != TaskStatus.RanToCompletion)
            await waitAsyncTask.ConfigureAwait(false);

        try
        {
            await next.SendAsync(context).ConfigureAwait(false);
        }
        finally
        {
            _limit.Release();
        }
    }

    /// <summary>Applies a non-stale broadcast adjustment while preserving the committed permit count.</summary>
    /// <param name="context">The timestamped control command and cancellation boundary.</param>
    /// <returns>The serialized adjustment of this anonymous pipeline limiter.</returns>
    public async Task SendAsync(CommandContext<SetConcurrencyLimit> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        SetConcurrencyLimit command = context.Command;
        ArgumentNullException.ThrowIfNull(command);
        if (command.LimiterId != null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(command.LimiterId);
            return;
        }

        var concurrencyLimit = command.ConcurrencyLimit;
        if (concurrencyLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(concurrencyLimit), "The concurrency limit must be >= 1");

        DateTimeOffset commandTimestamp = command.Timestamp ?? context.Timestamp;

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

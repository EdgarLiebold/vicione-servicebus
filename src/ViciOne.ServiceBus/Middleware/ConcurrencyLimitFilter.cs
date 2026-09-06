using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Limits the concurrency of the next section of the pipeline based on the concurrency limit
/// specified.
/// </summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class ConcurrencyLimitFilter<TContext> :
    Agent,
    IFilter<TContext>,
    IPipe<CommandContext<SetConcurrencyLimit>>,
    IDisposable
    where TContext : class, PipeContext
{
    readonly SemaphoreSlim _adjustment;
    readonly SemaphoreSlim _limit;
    int _concurrencyLimit;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="concurrencyLimit">The concurrency limit.</param>
    public ConcurrencyLimitFilter(int concurrencyLimit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(concurrencyLimit, 1);

        _concurrencyLimit = concurrencyLimit;

        _adjustment = new SemaphoreSlim(1, 1);
        _limit = new SemaphoreSlim(concurrencyLimit);
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        _adjustment.Dispose();
        _limit.Dispose();
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("concurrencyLimit");
        scope.Add("limit", Volatile.Read(ref _concurrencyLimit));
        scope.Add("available", _limit.CurrentCount);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [DebuggerNonUserCode]
    public async Task SendAsync(TContext context, IPipe<TContext> next)
    {
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

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(CommandContext<SetConcurrencyLimit> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var concurrencyLimit = context.Command.ConcurrencyLimit;
        if (concurrencyLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(concurrencyLimit), "The concurrency limit must be >= 1");

        await _adjustment.WaitAsync(context.CancellationToken).ConfigureAwait(false);
        try
        {
            int previousLimit = Volatile.Read(ref _concurrencyLimit);
            if (concurrencyLimit > previousLimit)
                _limit.Release(concurrencyLimit - previousLimit);
            else if (concurrencyLimit < previousLimit)
                await TakePermitsAsync(previousLimit - concurrencyLimit, context.CancellationToken).ConfigureAwait(false);

            Volatile.Write(ref _concurrencyLimit, concurrencyLimit);
        }
        finally
        {
            _adjustment.Release();
        }
    }

    /// <summary>Stops agent.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task StopAgentAsync(StopContext context)
    {
        var slot = 0;
        await _adjustment.WaitAsync(context.CancellationToken).ConfigureAwait(false);
        try
        {
            int concurrencyLimit = Volatile.Read(ref _concurrencyLimit);
            for (; slot < concurrencyLimit; slot++)
                await _limit.WaitAsync(context.CancellationToken).ConfigureAwait(false);

            await base.StopAgentAsync(context).ConfigureAwait(false);
        }
        finally
        {
            _limit.Release(slot);
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

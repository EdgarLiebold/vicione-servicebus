using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Limits the number of calls through the filter to a specified count per time interval
/// specified.
/// </summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class RateLimitFilter<TContext> :
    IFilter<TContext>,
    IPipe<CommandContext<SetRateLimit>>,
    IDisposable
    where TContext : class, PipeContext
{
    readonly SemaphoreSlim _adjustment;
    readonly TimeSpan _interval;
    readonly SemaphoreSlim _limit;
    readonly ITimer _timer;
    int _count;
    int _rateLimit;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="rateLimit">The rate limit.</param>
    /// <param name="interval">The interval.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public RateLimitFilter(int rateLimit, TimeSpan interval, TimeProvider? timeProvider = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rateLimit, 1);
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), interval, "The interval must be greater than zero.");

        _rateLimit = rateLimit;
        _interval = interval;
        _adjustment = new SemaphoreSlim(1, 1);
        _limit = new SemaphoreSlim(rateLimit);
        _timer = (timeProvider ?? TimeProvider.System).CreateTimer(Reset, null, interval, interval);
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        _timer.Dispose();
        _adjustment.Dispose();
        _limit.Dispose();
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("rateLimit");
        scope.Add("limit", Volatile.Read(ref _rateLimit));
        scope.Add("available", _limit.CurrentCount);
        scope.Add("interval", _interval);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [DebuggerNonUserCode]
    public Task SendAsync(TContext context, IPipe<TContext> next)
    {
        var waitAsync = _limit.WaitAsync(context.CancellationToken);
        if (waitAsync.Status == TaskStatus.RanToCompletion)
        {
            Interlocked.Increment(ref _count);

            return next.SendAsync(context);
        }

        async Task SendAsync()
        {
            await waitAsync.ConfigureAwait(false);

            Interlocked.Increment(ref _count);

            await next.SendAsync(context).ConfigureAwait(false);
        }

        return SendAsync();
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(CommandContext<SetRateLimit> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var rateLimit = context.Command.RateLimit;
        if (rateLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(rateLimit), "The rate limit must be >= 1");

        await _adjustment.WaitAsync(context.CancellationToken).ConfigureAwait(false);
        try
        {
            int previousLimit = Volatile.Read(ref _rateLimit);
            if (rateLimit > previousLimit)
                _limit.Release(rateLimit - previousLimit);
            else if (rateLimit < previousLimit)
                await TakePermitsAsync(previousLimit - rateLimit, context.CancellationToken).ConfigureAwait(false);

            Volatile.Write(ref _rateLimit, rateLimit);
        }
        finally
        {
            _adjustment.Release();
        }
    }

    void Reset(object? state)
    {
        var processed = Interlocked.Exchange(ref _count, 0);
        if (processed > 0)
            _limit.Release(processed);
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

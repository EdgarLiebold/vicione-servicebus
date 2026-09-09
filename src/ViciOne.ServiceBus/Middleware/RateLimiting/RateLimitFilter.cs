using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.Middleware.RateLimiting;

/// <summary>
/// Admits at most a configurable number of pipeline operations during each usage-anchored interval.
/// </summary>
/// <typeparam name="TContext">The context type carried by the pipeline.</typeparam>
internal sealed class RateLimitFilter<TContext> :
    IFilter<TContext>,
    IPipe<CommandContext<SetRateLimit>>
    where TContext : class, PipeContext
{
    readonly SemaphoreSlim _adjustment;
    readonly TimeSpan _interval;
    readonly SemaphoreSlim _limit;
    readonly object _stateLock;
    readonly TimeProvider _timeProvider;
    int _count;
    int _rateLimit;
    bool _windowActive;
    long _windowStartedAt;
    int _waiterCount;
    ITimer? _timer;
    long _timerGeneration;

    /// <summary>Creates a limiter whose first admitted operation starts each interval.</summary>
    /// <param name="rateLimit">The positive number of operations admitted per interval.</param>
    /// <param name="interval">The positive duration of an interval.</param>
    /// <param name="timeProvider">The time source used to schedule interval boundaries.</param>
    public RateLimitFilter(int rateLimit, TimeSpan interval, TimeProvider? timeProvider = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rateLimit, 1);
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), interval, "The interval must be greater than zero.");

        _rateLimit = rateLimit;
        _interval = interval;
        _adjustment = new SemaphoreSlim(1, 1);
        _limit = new SemaphoreSlim(rateLimit);
        _stateLock = new object();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Reports the configured limit, currently available permits, and interval.</summary>
    /// <param name="context">The probe that receives the limiter state.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateFilterScope("rateLimit");
        scope.Add("limit", Volatile.Read(ref _rateLimit));
        scope.Add("available", _limit.CurrentCount);
        scope.Add("interval", _interval);
    }

    /// <summary>Waits for admission and then invokes the next pipeline stage.</summary>
    /// <param name="context">The pipeline context whose cancellation stops admission.</param>
    /// <param name="next">The protected pipeline stage.</param>
    /// <returns>A task that completes with the protected stage.</returns>
    [DebuggerNonUserCode]
    public Task SendAsync(TContext context, IPipe<TContext> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var waitAsync = AcquireOperationPermitAsync(context.CancellationToken);
        if (waitAsync.Status == TaskStatus.RanToCompletion)
            return next.SendAsync(context);

        async Task AwaitPermitAndSendAsync()
        {
            await waitAsync.ConfigureAwait(false);
            await next.SendAsync(context).ConfigureAwait(false);
        }

        return AwaitPermitAndSendAsync();
    }

    /// <summary>Applies a positive rate-limit adjustment to this filter.</summary>
    /// <param name="context">The control command and cancellation boundary.</param>
    /// <returns>A task that completes when the new permit count is effective.</returns>
    public async Task SendAsync(CommandContext<SetRateLimit> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        SetRateLimit command = context.Command;
        ArgumentNullException.ThrowIfNull(command);

        int rateLimit = command.RateLimit;
        if (rateLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(rateLimit), rateLimit, "The rate limit must be at least one.");

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

    Task AcquireOperationPermitAsync(CancellationToken cancellationToken)
    {
        ITimer? expiredTimer = null;
        var acquired = false;
        try
        {
            lock (_stateLock)
            {
                expiredTimer = RefreshExpiredWindowLocked();
                if (_limit.Wait(0, cancellationToken))
                {
                    try
                    {
                        RecordConsumptionLocked();
                        acquired = true;
                    }
                    catch
                    {
                        _limit.Release();
                        throw;
                    }
                }
                else
                    RegisterWaiterLocked();
            }
        }
        finally
        {
            DisposeTimer(expiredTimer);
        }

        return acquired ? Task.CompletedTask : AcquireOperationPermitSlowAsync(cancellationToken);
    }

    async Task AcquireOperationPermitSlowAsync(CancellationToken cancellationToken)
    {
        var acquired = false;
        var registeredWaiter = true;
        try
        {
            await _limit.WaitAsync(cancellationToken).ConfigureAwait(false);
            acquired = true;
            registeredWaiter = false;
            ConvertWaiterToConsumption();
        }
        catch
        {
            if (acquired)
                _limit.Release();

            throw;
        }
        finally
        {
            if (registeredWaiter)
                UnregisterWaiter();
        }
    }

    Task ReservePermitAsync(CancellationToken cancellationToken)
    {
        ITimer? expiredTimer = null;
        var acquired = false;
        try
        {
            lock (_stateLock)
            {
                expiredTimer = RefreshExpiredWindowLocked();
                if (_limit.Wait(0, cancellationToken))
                    acquired = true;
                else
                    RegisterWaiterLocked();
            }
        }
        finally
        {
            DisposeTimer(expiredTimer);
        }

        return acquired ? Task.CompletedTask : ReservePermitSlowAsync(cancellationToken);
    }

    async Task ReservePermitSlowAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _limit.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            UnregisterWaiter();
        }
    }

    ITimer? RefreshExpiredWindowLocked()
    {
        if (!_windowActive || _timeProvider.GetElapsedTime(_windowStartedAt) < _interval)
            return null;

        ITimer? timer = _timer;
        _timer = null;
        int processed = _count;
        _count = 0;
        _windowActive = false;

        if (processed > 0)
            _limit.Release(processed);

        return timer;
    }

    void Reset(object? state)
    {
        if (state is not long timerGeneration)
            return;

        ITimer? timer = null;
        int processed = 0;
        lock (_stateLock)
        {
            if (_timer == null || timerGeneration != _timerGeneration)
                return;

            timer = _timer;
            _timer = null;
            processed = _count;
            _count = 0;
            _windowActive = false;
        }

        DisposeTimer(timer);
        if (processed > 0)
            _limit.Release(processed);
    }

    void RecordConsumptionLocked()
    {
        if (!_windowActive)
        {
            _windowStartedAt = _timeProvider.GetTimestamp();
            _windowActive = true;
        }

        _count++;
        try
        {
            if (_waiterCount > 0 && _timer == null)
                CreateTimer();
        }
        catch
        {
            _count--;
            if (_count == 0)
                _windowActive = false;
            throw;
        }
    }

    void RegisterWaiterLocked()
    {
        _waiterCount++;
        try
        {
            if (_windowActive && _timer == null)
                CreateTimer();
        }
        catch
        {
            _waiterCount--;
            throw;
        }
    }

    void ConvertWaiterToConsumption()
    {
        ITimer? timer = null;
        try
        {
            lock (_stateLock)
            {
                _waiterCount--;
                try
                {
                    RecordConsumptionLocked();
                }
                finally
                {
                    if (_waiterCount == 0)
                    {
                        timer = _timer;
                        _timer = null;
                    }
                }
            }
        }
        finally
        {
            DisposeTimer(timer);
        }
    }

    void UnregisterWaiter()
    {
        ITimer? timer = null;
        lock (_stateLock)
        {
            _waiterCount--;
            if (_waiterCount == 0)
            {
                timer = _timer;
                _timer = null;
            }
        }

        DisposeTimer(timer);
    }

    void CreateTimer()
    {
        TimeSpan dueTime = _windowActive
            ? _interval - _timeProvider.GetElapsedTime(_windowStartedAt)
            : _interval;
        if (dueTime < TimeSpan.Zero)
            dueTime = TimeSpan.Zero;

        long timerGeneration = ++_timerGeneration;
        _timer = _timeProvider.CreateTimer(Reset, timerGeneration, dueTime, System.Threading.Timeout.InfiniteTimeSpan)
            ?? throw new InvalidOperationException("The time provider returned no timer.");
    }

    static void DisposeTimer(ITimer? timer)
    {
        try
        {
            timer?.Dispose();
        }
        catch (Exception exception)
        {
            try
            {
                LogContext.Warning?.Log(exception, "Rate-limit interval timer disposal faulted");
            }
            catch
            {
                // Diagnostic logging cannot change rate-limit admission or timer-callback outcomes.
            }
        }
    }

    async Task TakePermitsAsync(int count, CancellationToken cancellationToken)
    {
        var acquired = 0;
        try
        {
            for (; acquired < count; acquired++)
                await ReservePermitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            if (acquired > 0)
                _limit.Release(acquired);

            throw;
        }
    }
}

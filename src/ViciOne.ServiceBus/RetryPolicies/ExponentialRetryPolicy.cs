using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Retries handled failures with bounded exponentially increasing jittered delays.</summary>
internal sealed class ExponentialRetryPolicy :
    IRetryPolicy
{
    readonly IExceptionFilter _filter;
    readonly long _upperDeltaTicks;
    readonly TimeSpan[] _intervals;
    readonly long _lowerDeltaTicks;
    readonly long _maximumIntervalTicks;
    readonly long _minimumIntervalTicks;

    /// <summary>Creates a bounded exponential retry policy.</summary>
    /// <param name="filter">Determines which exceptions are retried.</param>
    /// <param name="retryLimit">The maximum number of retry attempts.</param>
    /// <param name="minInterval">The minimum retry delay.</param>
    /// <param name="maxInterval">The maximum retry delay.</param>
    /// <param name="intervalDelta">The base exponential delay increment.</param>
    public ExponentialRetryPolicy(IExceptionFilter filter, int retryLimit, TimeSpan minInterval, TimeSpan maxInterval, TimeSpan intervalDelta)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retryLimit);
        if (minInterval < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(minInterval), "The minimum interval must be non-negative.");
        if (maxInterval < minInterval)
            throw new ArgumentOutOfRangeException(nameof(maxInterval), "The maximum interval must not be less than the minimum interval.");
        if (intervalDelta <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(intervalDelta), "The interval delta must be positive.");
        if (maxInterval.TotalMilliseconds > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(maxInterval), "The maximum interval must not exceed Int32.MaxValue milliseconds.");
        if (intervalDelta.TotalMilliseconds > int.MaxValue / 1.2)
            throw new ArgumentOutOfRangeException(nameof(intervalDelta), "The interval delta is too large.");

        _filter = filter;
        RetryLimit = retryLimit;
        _minimumIntervalTicks = minInterval.Ticks;
        _maximumIntervalTicks = maxInterval.Ticks;

        _lowerDeltaTicks = Math.Max(1, (long)(intervalDelta.Ticks * 0.8));
        _upperDeltaTicks = Math.Max(_lowerDeltaTicks + 1, (long)Math.Ceiling(intervalDelta.Ticks * 1.2));

        _intervals = CalculateIntervals().ToArray();
    }

    /// <summary>Gets the maximum number of retry attempts.</summary>
    public int RetryLimit { get; }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Set(new
        {
            Policy = "Exponential",
            Limit = RetryLimit,
            Min = TimeSpan.FromTicks(_minimumIntervalTicks).TotalMilliseconds,
            Max = TimeSpan.FromTicks(_maximumIntervalTicks).TotalMilliseconds,
            Low = TimeSpan.FromTicks(_lowerDeltaTicks).TotalMilliseconds,
            High = TimeSpan.FromTicks(_upperDeltaTicks).TotalMilliseconds
        });

        _filter.Probe(context);
    }

    RetryPolicyContext<T> IRetryPolicy.CreatePolicyContext<T>(T context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new ExponentialRetryPolicyContext<T>(this, context);
    }

    /// <summary>Determines whether the configured exception filter handles a failure.</summary>
    /// <param name="exception">The failure to classify.</param>
    /// <returns><see langword="true" /> when the failure is eligible for retry; otherwise, <see langword="false" />.</returns>
    public bool IsHandled(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return _filter.Match(exception);
    }

    /// <summary>Gets a bounded jittered delay for a zero-based retry count.</summary>
    /// <param name="retryCount">The zero-based retry count.</param>
    /// <returns>The retry delay.</returns>
    public TimeSpan GetRetryInterval(int retryCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(retryCount);

        var interval = retryCount < _intervals.Length ? _intervals[retryCount] : _intervals[_intervals.Length - 1];
        var jitter = Random.Shared.NextDouble() * 0.5 + 0.75;
        var ticks = Math.Clamp((long)(interval.Ticks * jitter), _minimumIntervalTicks, _maximumIntervalTicks);

        return TimeSpan.FromTicks(ticks);
    }

    IEnumerable<TimeSpan> CalculateIntervals()
    {
        long intervalTicks = -1;

        for (var i = 0; i < RetryLimit && intervalTicks < _maximumIntervalTicks; i++)
        {
            double exponentialTicks = _minimumIntervalTicks
                + Math.Pow(2, i) * Random.Shared.NextInt64(_lowerDeltaTicks, _upperDeltaTicks);
            intervalTicks = Math.Clamp((long)Math.Min(exponentialTicks, _maximumIntervalTicks),
                _minimumIntervalTicks, _maximumIntervalTicks);

            yield return TimeSpan.FromTicks(intervalTicks);
        }
    }

    /// <summary>Returns a diagnostic description of the exponential schedule.</summary>
    /// <returns>The policy name, retry limit, and delay bounds.</returns>
    public override string ToString()
    {
        return FormattableString.Invariant($"Exponential (limit {RetryLimit}, min {TimeSpan.FromTicks(_minimumIntervalTicks).TotalMilliseconds}ms, max {TimeSpan.FromTicks(_maximumIntervalTicks).TotalMilliseconds}ms)");
    }
}

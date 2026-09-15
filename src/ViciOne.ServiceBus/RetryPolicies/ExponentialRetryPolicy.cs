using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Retries handled failures with bounded exponentially increasing jittered delays.</summary>
internal sealed class ExponentialRetryPolicy :
    IRetryPolicy
{
    readonly IExceptionFilter _filter;
    readonly int _highInterval;
    readonly TimeSpan[] _intervals;
    readonly int _lowInterval;
    readonly int _maxInterval;
    readonly int _minInterval;

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
        _minInterval = (int)minInterval.TotalMilliseconds;
        _maxInterval = (int)maxInterval.TotalMilliseconds;

        _lowInterval = (int)(intervalDelta.TotalMilliseconds * 0.8);
        _highInterval = (int)(intervalDelta.TotalMilliseconds * 1.2);

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
            Min = _minInterval,
            Max = _maxInterval,
            Low = _lowInterval,
            High = _highInterval
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
        var milliseconds = Math.Clamp(interval.TotalMilliseconds * jitter, _minInterval, _maxInterval);

        return TimeSpan.FromMilliseconds(milliseconds);
    }

    IEnumerable<TimeSpan> CalculateIntervals()
    {
        var delta = -1;

        for (var i = 0; i < RetryLimit && delta < _maxInterval; i++)
        {
            delta = (int)Math.Min(_minInterval + Math.Pow(2, i) * Random.Shared.Next(_lowInterval, _highInterval), _maxInterval);

            yield return TimeSpan.FromMilliseconds(delta);
        }
    }

    /// <summary>Returns a diagnostic description of the exponential schedule.</summary>
    /// <returns>The policy name, retry limit, and delay bounds.</returns>
    public override string ToString()
    {
        return $"Exponential (limit {RetryLimit}, min {_minInterval}ms, max {_maxInterval}ms)";
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Retries handled failures according to an explicit delay schedule.</summary>
internal sealed class IntervalRetryPolicy :
    IRetryPolicy
{
    readonly IExceptionFilter _filter;

    /// <summary>Creates an interval retry policy.</summary>
    /// <param name="filter">Determines which exceptions are retried.</param>
    /// <param name="intervals">The delay before each retry attempt.</param>
    public IntervalRetryPolicy(IExceptionFilter filter, params TimeSpan[] intervals)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(intervals);
        if (intervals.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(intervals), "At least one interval must be specified");
        if (intervals.Any(interval => interval < TimeSpan.Zero))
            throw new ArgumentOutOfRangeException(nameof(intervals), "Retry intervals must be non-negative.");

        _filter = filter;
        Intervals = Array.AsReadOnly([.. intervals]);
    }

    /// <summary>Gets the immutable retry-delay schedule.</summary>
    public IReadOnlyList<TimeSpan> Intervals { get; }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Set(new
        {
            Policy = "Interval",
            Limit = Intervals.Count,
            Intervals
        });

        _filter.Probe(context);
    }

    RetryPolicyContext<T> IRetryPolicy.CreatePolicyContext<T>(T context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new IntervalRetryPolicyContext<T>(this, context);
    }

    /// <summary>Determines whether the configured exception filter handles a failure.</summary>
    /// <param name="exception">The failure to classify.</param>
    /// <returns><see langword="true" /> when the failure is eligible for retry; otherwise, <see langword="false" />.</returns>
    public bool IsHandled(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return _filter.Match(exception);
    }

    /// <summary>Returns a diagnostic description of the interval schedule.</summary>
    /// <returns>The policy name, retry limit, and first configured intervals.</returns>
    public override string ToString()
    {
        return $"Interval (limit {Intervals.Count}, intervals {string.Join(";", Intervals.Take(5).Select(x => x.ToString()))})";
    }
}

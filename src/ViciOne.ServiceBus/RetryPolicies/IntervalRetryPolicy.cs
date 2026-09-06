using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Defines policy for interval retry.</summary>
public class IntervalRetryPolicy :
    IRetryPolicy
{
    readonly IExceptionFilter _filter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="intervals">The intervals.</param>
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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="intervals">The intervals.</param>
    public IntervalRetryPolicy(IExceptionFilter filter, params int[] intervals)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(intervals);
        if (intervals.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(intervals), "At least one interval must be specified");
        if (intervals.Any(interval => interval < 0))
            throw new ArgumentOutOfRangeException(nameof(intervals), "Retry intervals must be non-negative.");

        _filter = filter;
        Intervals = Array.AsReadOnly(intervals.Select(x => TimeSpan.FromMilliseconds(x)).ToArray());
    }

    /// <summary>Gets the intervals.</summary>
    public IReadOnlyList<TimeSpan> Intervals { get; }

    void IProbeSite.Probe(ProbeContext context)
    {
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
        return new IntervalRetryPolicyContext<T>(this, context);
    }

    /// <summary>Determines whether handled.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsHandled(Exception exception)
    {
        return _filter.Match(exception);
    }

    /// <summary>Returns the string representation of this instance.</summary>
    /// <returns>The converted string.</returns>
    public override string ToString()
    {
        return $"Interval (limit {Intervals.Count}, intervals {string.Join(";", Intervals.Take(5).Select(x => x.ToString()))})";
    }
}

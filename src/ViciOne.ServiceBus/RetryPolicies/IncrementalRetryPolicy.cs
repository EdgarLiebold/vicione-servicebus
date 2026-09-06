using System;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Defines policy for incremental retry.</summary>
public class IncrementalRetryPolicy :
    IRetryPolicy
{
    readonly IExceptionFilter _filter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="retryLimit">The retry limit.</param>
    /// <param name="initialInterval">The initial interval.</param>
    /// <param name="intervalIncrement">The interval increment.</param>
    public IncrementalRetryPolicy(IExceptionFilter filter, int retryLimit, TimeSpan initialInterval,
        TimeSpan intervalIncrement)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retryLimit);

        if (initialInterval < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(initialInterval),
                "The initial interval must be non-negative.");
        }

        if (intervalIncrement < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalIncrement),
                "The interval increment must be non-negative.");
        }

        if (retryLimit > 1
            && intervalIncrement.Ticks > (TimeSpan.MaxValue.Ticks - initialInterval.Ticks) / (retryLimit - 1L))
        {
            throw new ArgumentOutOfRangeException(nameof(intervalIncrement),
                "The retry interval would exceed TimeSpan.MaxValue before the configured retry limit.");
        }

        _filter = filter;
        RetryLimit = retryLimit;
        InitialInterval = initialInterval;
        IntervalIncrement = intervalIncrement;
    }

    /// <summary>Gets the retry limit.</summary>
    public int RetryLimit { get; }

    /// <summary>Gets the initial interval.</summary>
    public TimeSpan InitialInterval { get; }

    /// <summary>Gets the interval increment.</summary>
    public TimeSpan IntervalIncrement { get; }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.Set(new
        {
            Policy = "Incremental",
            Limit = RetryLimit,
            Initial = InitialInterval,
            Increment = IntervalIncrement
        });

        _filter.Probe(context);
    }

    RetryPolicyContext<T> IRetryPolicy.CreatePolicyContext<T>(T context)
    {
        return new IncrementalRetryPolicyContext<T>(this, context);
    }

    /// <summary>Determines whether handled.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsHandled(Exception exception)
    {
        return _filter.Match(exception);
    }
}

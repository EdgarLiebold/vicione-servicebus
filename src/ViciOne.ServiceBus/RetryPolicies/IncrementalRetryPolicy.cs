using System;

namespace ViciOne.ServiceBus.RetryPolicies;

public class IncrementalRetryPolicy :
    IRetryPolicy
{
    readonly IExceptionFilter _filter;

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

    public int RetryLimit { get; }

    public TimeSpan InitialInterval { get; }

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

    public bool IsHandled(Exception exception)
    {
        return _filter.Match(exception);
    }
}

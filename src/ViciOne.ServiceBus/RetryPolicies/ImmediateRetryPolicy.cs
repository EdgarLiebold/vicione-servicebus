using System;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>
/// Provides an immediate retry policy implementation.
/// </summary>
public class ImmediateRetryPolicy :
    IRetryPolicy
{
    readonly IExceptionFilter _filter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    /// <param name="retryLimit">The retry limit value.</param>
    public ImmediateRetryPolicy(IExceptionFilter filter, int retryLimit)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retryLimit);

        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
        RetryLimit = retryLimit;
    }

    /// <summary>
    /// Gets the retry limit value.
    /// </summary>
    public int RetryLimit { get; }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.Set(new
        {
            Policy = "Immediate",
            Limit = RetryLimit
        });

        _filter.Probe(context);
    }

    RetryPolicyContext<T> IRetryPolicy.CreatePolicyContext<T>(T context)
    {
        return new ImmediateRetryPolicyContext<T>(this, context);
    }

    /// <summary>
    /// Determines whether handled.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsHandled(Exception exception)
    {
        return _filter.Match(exception);
    }
}

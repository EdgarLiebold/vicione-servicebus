using System;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Retries handled failures without a delay.</summary>
public sealed class ImmediateRetryPolicy :
    IRetryPolicy
{
    readonly IExceptionFilter _filter;

    /// <summary>Creates an immediate retry policy.</summary>
    /// <param name="filter">Determines which exceptions are retried.</param>
    /// <param name="retryLimit">The maximum number of retry attempts.</param>
    public ImmediateRetryPolicy(IExceptionFilter filter, int retryLimit)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retryLimit);

        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
        RetryLimit = retryLimit;
    }

    /// <summary>Gets the retry limit.</summary>
    public int RetryLimit { get; }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Set(new
        {
            Policy = "Immediate",
            Limit = RetryLimit
        });

        _filter.Probe(context);
    }

    RetryPolicyContext<T> IRetryPolicy.CreatePolicyContext<T>(T context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new ImmediateRetryPolicyContext<T>(this, context);
    }

    /// <summary>Determines whether handled.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsHandled(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return _filter.Match(exception);
    }
}

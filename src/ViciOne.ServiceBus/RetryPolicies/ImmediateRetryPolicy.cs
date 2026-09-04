using System;

namespace ViciOne.ServiceBus.RetryPolicies;

public class ImmediateRetryPolicy :
    IRetryPolicy
{
    readonly IExceptionFilter _filter;

    public ImmediateRetryPolicy(IExceptionFilter filter, int retryLimit)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retryLimit);

        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
        RetryLimit = retryLimit;
    }

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

    public bool IsHandled(Exception exception)
    {
        return _filter.Match(exception);
    }
}

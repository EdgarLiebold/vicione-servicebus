using System;
using System.Threading;

namespace ViciOne.ServiceBus.RetryPolicies;

public class IncrementalRetryPolicyContext<TContext> :
    BaseRetryPolicyContext<TContext>
    where TContext : class, PipeContext
{
    readonly IncrementalRetryPolicy _policy;

    public IncrementalRetryPolicyContext(IncrementalRetryPolicy policy, TContext context)
        : base(policy, context)
    {
        _policy = policy;
    }

    protected override RetryContext<TContext> CreateRetryContext(Exception exception, CancellationToken cancellationToken)
    {
        return new IncrementalRetryContext<TContext>(_policy, Context, exception, 0, _policy.InitialInterval, _policy.IntervalIncrement, cancellationToken);
    }
}

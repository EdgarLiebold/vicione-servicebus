using System;
using System.Threading;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Carries state for immediate retry policy operations.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class ImmediateRetryPolicyContext<TContext> :
    BaseRetryPolicyContext<TContext>
    where TContext : class, PipeContext
{
    readonly ImmediateRetryPolicy _policy;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="policy">The policy.</param>
    /// <param name="context">The context associated with the operation.</param>
    public ImmediateRetryPolicyContext(ImmediateRetryPolicy policy, TContext context)
        : base(policy, context)
    {
        _policy = policy;
    }

    /// <summary>Creates retry context.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created retry context.</returns>
    protected override RetryContext<TContext> CreateRetryContext(Exception exception, CancellationToken cancellationToken)
    {
        return new ImmediateRetryContext<TContext>(_policy, Context, exception, 0, cancellationToken);
    }
}

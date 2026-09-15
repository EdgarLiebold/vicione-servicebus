using System;
using System.Threading;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Creates initial decisions for an immediate retry policy.</summary>
/// <typeparam name="TContext">The pipeline context type.</typeparam>
internal sealed class ImmediateRetryPolicyContext<TContext> :
    BaseRetryPolicyContext<TContext>
    where TContext : class, PipeContext
{
    readonly ImmediateRetryPolicy _policy;

    /// <summary>Creates operation-scoped state for an immediate retry policy.</summary>
    /// <param name="policy">The immediate retry policy.</param>
    /// <param name="context">The pipeline context governed by the policy.</param>
    public ImmediateRetryPolicyContext(ImmediateRetryPolicy policy, TContext context)
        : base(policy, context)
    {
        _policy = policy;
    }

    /// <summary>Creates the initial immediate decision.</summary>
    /// <param name="exception">The initial failure.</param>
    /// <param name="cancellationToken">The token that cancels retry processing.</param>
    /// <param name="isRetryScheduled"><see langword="true" /> when another attempt is scheduled.</param>
    /// <returns>The initial retry state.</returns>
    protected override RetryContext<TContext> CreateRetryContext(Exception exception, CancellationToken cancellationToken,
        bool isRetryScheduled)
    {
        return new ImmediateRetryContext<TContext>(_policy, Context, exception, 0, cancellationToken);
    }
}

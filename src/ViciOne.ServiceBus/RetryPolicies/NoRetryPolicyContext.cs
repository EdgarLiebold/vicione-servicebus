using System;
using System.Threading;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Creates terminal retry state for a policy that never retries.</summary>
/// <typeparam name="TContext">The pipeline context type.</typeparam>
internal sealed class NoRetryPolicyContext<TContext> :
    BaseRetryPolicyContext<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Creates operation-scoped state for the supplied no-retry policy.</summary>
    /// <param name="policy">The no-retry policy.</param>
    /// <param name="context">The pipeline context governed by the policy.</param>
    public NoRetryPolicyContext(IRetryPolicy policy, TContext context)
        : base(policy, context)
    {
    }

    /// <summary>Creates terminal state for the supplied failure.</summary>
    /// <param name="exception">The most recent failure.</param>
    /// <param name="retryContext">The terminal retry state.</param>
    /// <returns>Always <see langword="false" />.</returns>
    public override bool CanRetry(Exception exception, out RetryContext<TContext> retryContext)
    {
        retryContext = new NoRetryContext<TContext>(Context, exception, CancellationToken);

        return false;
    }

    /// <summary>Creates the initial terminal state.</summary>
    /// <param name="exception">The initial failure.</param>
    /// <param name="cancellationToken">The token that cancels retry processing.</param>
    /// <param name="isRetryScheduled">Always <see langword="false" /> for this policy.</param>
    /// <returns>The terminal retry state.</returns>
    protected override RetryContext<TContext> CreateRetryContext(Exception exception, CancellationToken cancellationToken,
        bool isRetryScheduled)
    {
        return new NoRetryContext<TContext>(Context, exception, cancellationToken);
    }
}

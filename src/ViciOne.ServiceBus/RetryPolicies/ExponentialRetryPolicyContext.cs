using System;
using System.Threading;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Creates initial decisions for a bounded exponential retry schedule.</summary>
/// <typeparam name="TContext">The pipeline context type.</typeparam>
internal sealed class ExponentialRetryPolicyContext<TContext> :
    BaseRetryPolicyContext<TContext>
    where TContext : class, PipeContext
{
    readonly ExponentialRetryPolicy _policy;

    /// <summary>Creates operation-scoped state for an exponential retry policy.</summary>
    /// <param name="policy">The exponential retry policy.</param>
    /// <param name="context">The pipeline context governed by the policy.</param>
    public ExponentialRetryPolicyContext(ExponentialRetryPolicy policy, TContext context)
        : base(policy, context)
    {
        _policy = policy;
    }

    /// <summary>Creates the initial exponential decision.</summary>
    /// <param name="exception">The initial failure.</param>
    /// <param name="cancellationToken">The token that cancels retry processing.</param>
    /// <param name="isRetryScheduled"><see langword="true" /> when another attempt is scheduled.</param>
    /// <returns>The initial retry state.</returns>
    protected override RetryContext<TContext> CreateRetryContext(Exception exception, CancellationToken cancellationToken,
        bool isRetryScheduled)
    {
        TimeSpan? delay = isRetryScheduled ? _policy.GetRetryInterval(0) : null;
        return new ExponentialRetryContext<TContext>(_policy, Context, exception, 0, delay, cancellationToken);
    }
}

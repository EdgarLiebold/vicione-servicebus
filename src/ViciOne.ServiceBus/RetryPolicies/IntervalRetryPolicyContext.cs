using System;
using System.Threading;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Creates initial decisions for an explicit interval schedule.</summary>
/// <typeparam name="TContext">The pipeline context type.</typeparam>
internal sealed class IntervalRetryPolicyContext<TContext> :
    BaseRetryPolicyContext<TContext>
    where TContext : class, PipeContext
{
    readonly IntervalRetryPolicy _policy;

    /// <summary>Creates operation-scoped state for an interval retry policy.</summary>
    /// <param name="policy">The interval retry policy.</param>
    /// <param name="context">The pipeline context governed by the policy.</param>
    public IntervalRetryPolicyContext(IntervalRetryPolicy policy, TContext context)
        : base(policy, context)
    {
        _policy = policy;
    }

    /// <summary>Creates the initial interval decision.</summary>
    /// <param name="exception">The initial failure.</param>
    /// <param name="cancellationToken">The token that cancels retry processing.</param>
    /// <param name="isRetryScheduled"><see langword="true" /> when another attempt is scheduled.</param>
    /// <returns>The initial retry state.</returns>
    protected override RetryContext<TContext> CreateRetryContext(Exception exception, CancellationToken cancellationToken,
        bool isRetryScheduled)
    {
        TimeSpan? delay = isRetryScheduled ? _policy.Intervals[0] : null;
        return new IntervalRetryContext<TContext>(_policy, Context, exception, 0, delay, cancellationToken);
    }
}

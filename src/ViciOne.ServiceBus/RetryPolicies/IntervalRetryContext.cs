using System;
using System.Threading;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Carries state for interval retry operations.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class IntervalRetryContext<TContext> :
    BaseRetryContext<TContext>,
    RetryContext<TContext>
    where TContext : class, PipeContext
{
    readonly IntervalRetryPolicy _policy;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="policy">The policy.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="retryCount">The retry count.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public IntervalRetryContext(IntervalRetryPolicy policy, TContext context, Exception exception, int retryCount, CancellationToken cancellationToken)
        : base(context, exception, retryCount, cancellationToken)
    {
        _policy = policy;
    }

    /// <summary>Gets the delay.</summary>
    public override TimeSpan? Delay => _policy.Intervals[RetryCount];

    bool RetryContext<TContext>.CanRetry(Exception exception, out RetryContext<TContext> retryContext)
    {
        retryContext = new IntervalRetryContext<TContext>(_policy, Context, Exception, RetryCount + 1, CancellationToken);

        return RetryAttempt < _policy.Intervals.Count && _policy.IsHandled(exception);
    }
}

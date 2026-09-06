using System;
using System.Threading;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Carries state for incremental retry operations.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class IncrementalRetryContext<TContext> :
    BaseRetryContext<TContext>,
    RetryContext<TContext>
    where TContext : class, PipeContext
{
    readonly TimeSpan _delay;
    readonly TimeSpan _delayIncrement;
    readonly IncrementalRetryPolicy _policy;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="policy">The policy.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="retryCount">The retry count.</param>
    /// <param name="delay">The delay before the operation is attempted.</param>
    /// <param name="delayIncrement">The delay increment.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public IncrementalRetryContext(IncrementalRetryPolicy policy, TContext context, Exception exception, int retryCount, TimeSpan delay,
        TimeSpan delayIncrement, CancellationToken cancellationToken)
        : base(context, exception, retryCount, cancellationToken)
    {
        _policy = policy;
        _delay = delay;
        _delayIncrement = delayIncrement;
    }

    /// <summary>Gets the delay.</summary>
    public override TimeSpan? Delay => _delay;

    bool RetryContext<TContext>.CanRetry(Exception exception, out RetryContext<TContext> retryContext)
    {
        retryContext = new IncrementalRetryContext<TContext>(_policy, Context, Exception, RetryCount + 1, _delay + _delayIncrement, _delayIncrement,
            CancellationToken);

        return RetryAttempt < _policy.RetryLimit && _policy.IsHandled(exception);
    }
}

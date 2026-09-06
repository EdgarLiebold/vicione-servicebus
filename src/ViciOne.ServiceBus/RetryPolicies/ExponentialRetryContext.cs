using System;
using System.Threading;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Carries state for exponential retry operations.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class ExponentialRetryContext<TContext> :
    BaseRetryContext<TContext>,
    RetryContext<TContext>
    where TContext : class, PipeContext
{
    readonly TimeSpan _delay;
    readonly ExponentialRetryPolicy _policy;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="policy">The policy.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="retryCount">The retry count.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public ExponentialRetryContext(ExponentialRetryPolicy policy, TContext context, Exception exception, int retryCount,
        CancellationToken cancellationToken)
        : base(context, exception, retryCount, cancellationToken)
    {
        _policy = policy;
        _delay = policy.GetRetryInterval(retryCount);
    }

    /// <summary>Gets the delay.</summary>
    public override TimeSpan? Delay => _delay;

    bool RetryContext<TContext>.CanRetry(Exception exception, out RetryContext<TContext> retryContext)
    {
        retryContext = new ExponentialRetryContext<TContext>(_policy, Context, Exception, RetryCount + 1, CancellationToken);

        return RetryAttempt < _policy.RetryLimit && _policy.IsHandled(exception);
    }
}

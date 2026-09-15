using System;
using System.Threading;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Represents an immediate retry decision.</summary>
/// <typeparam name="TContext">The pipeline context type.</typeparam>
internal sealed class ImmediateRetryContext<TContext> :
    BaseRetryContext<TContext>,
    RetryContext<TContext>
    where TContext : class, PipeContext
{
    readonly ImmediateRetryPolicy _policy;

    /// <summary>Creates an immediate decision for a failed attempt.</summary>
    /// <param name="policy">The immediate retry policy.</param>
    /// <param name="context">The failed pipeline context.</param>
    /// <param name="exception">The most recent failure.</param>
    /// <param name="retryCount">The number of retry attempts already completed.</param>
    /// <param name="cancellationToken">The token that cancels retry processing.</param>
    public ImmediateRetryContext(ImmediateRetryPolicy policy, TContext context, Exception exception, int retryCount, CancellationToken cancellationToken)
        : base(context, exception, retryCount, cancellationToken)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    bool RetryContext<TContext>.CanRetry(Exception exception, out RetryContext<TContext> retryContext)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var canRetry = RetryAttempt < _policy.RetryLimit && _policy.IsHandled(exception);
        retryContext = new ImmediateRetryContext<TContext>(_policy, Context, exception, RetryCount + 1, CancellationToken);

        return canRetry;
    }
}

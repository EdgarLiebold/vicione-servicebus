using System;
using System.Threading;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Represents a decision in a linearly increasing retry schedule.</summary>
/// <typeparam name="TContext">The pipeline context type.</typeparam>
internal sealed class IncrementalRetryContext<TContext> :
    BaseRetryContext<TContext>,
    RetryContext<TContext>
    where TContext : class, PipeContext
{
    readonly TimeSpan? _delay;
    readonly TimeSpan _delayIncrement;
    readonly IncrementalRetryPolicy _policy;

    /// <summary>Creates a retry decision for a failed attempt.</summary>
    /// <param name="policy">The incremental retry policy.</param>
    /// <param name="context">The failed pipeline context.</param>
    /// <param name="exception">The most recent failure.</param>
    /// <param name="retryCount">The number of retry attempts already completed.</param>
    /// <param name="delay">The delay before the represented retry, or <see langword="null" /> when no retry is scheduled.</param>
    /// <param name="delayIncrement">The interval added before each subsequent retry.</param>
    /// <param name="cancellationToken">The token that cancels retry processing.</param>
    public IncrementalRetryContext(IncrementalRetryPolicy policy, TContext context, Exception exception, int retryCount, TimeSpan? delay,
        TimeSpan delayIncrement, CancellationToken cancellationToken)
        : base(context, exception, retryCount, cancellationToken)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _delay = delay;
        _delayIncrement = delayIncrement;
    }

    /// <summary>Gets the delay before the represented retry, or <see langword="null" /> for a terminal decision.</summary>
    public override TimeSpan? Delay => _delay;

    bool RetryContext<TContext>.CanRetry(Exception exception, out RetryContext<TContext> retryContext)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var nextRetryCount = RetryCount + 1;
        var canRetry = RetryAttempt < _policy.RetryLimit && _policy.IsHandled(exception);
        TimeSpan? nextDelay = canRetry ? _delay!.Value + _delayIncrement : null;
        retryContext = new IncrementalRetryContext<TContext>(_policy, Context, exception, nextRetryCount, nextDelay, _delayIncrement,
            CancellationToken);

        return canRetry;
    }
}

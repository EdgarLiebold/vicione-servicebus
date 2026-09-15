using System;
using System.Threading;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Represents the terminal state produced by a policy that never retries.</summary>
/// <typeparam name="TContext">The pipeline context type.</typeparam>
internal sealed class NoRetryContext<TContext> :
    BaseRetryContext<TContext>,
    RetryContext<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Creates a terminal state for the failed operation.</summary>
    /// <param name="context">The failed pipeline context.</param>
    /// <param name="exception">The most recent failure.</param>
    /// <param name="cancellationToken">The token that cancels retry processing.</param>
    public NoRetryContext(TContext context, Exception exception, CancellationToken cancellationToken)
        : base(context, exception, 0, cancellationToken)
    {
    }

    bool RetryContext<TContext>.CanRetry(Exception exception, out RetryContext<TContext> retryContext)
    {
        ArgumentNullException.ThrowIfNull(exception);

        retryContext = ReferenceEquals(exception, Exception)
            ? this
            : new NoRetryContext<TContext>(Context, exception, CancellationToken);

        return false;
    }
}

using System;
using System.Threading;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>
/// Provides a no retry context implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public class NoRetryContext<TContext> :
    BaseRetryContext<TContext>,
    RetryContext<TContext>
    where TContext : class, PipeContext
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public NoRetryContext(TContext context, Exception exception, CancellationToken cancellationToken)
        : base(context, exception, 0, cancellationToken)
    {
    }

    bool RetryContext<TContext>.CanRetry(Exception exception, out RetryContext<TContext> retryContext)
    {
        retryContext = this;

        return false;
    }
}

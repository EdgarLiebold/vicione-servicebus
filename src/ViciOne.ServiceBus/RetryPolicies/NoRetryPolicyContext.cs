using System;
using System.Threading;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>
/// Provides a no retry policy context implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public class NoRetryPolicyContext<TContext> :
    BaseRetryPolicyContext<TContext>
    where TContext : class, PipeContext
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="policy">The policy value.</param>
    /// <param name="context">The operation context.</param>
    public NoRetryPolicyContext(IRetryPolicy policy, TContext context)
        : base(policy, context)
    {
    }

    /// <summary>
    /// Determines whether the current value can retry.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="retryContext">The retry context value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool CanRetry(Exception exception, out RetryContext<TContext> retryContext)
    {
        retryContext = new NoRetryContext<TContext>(Context, exception, CancellationToken);

        return false;
    }

    /// <summary>
    /// Creates retry context.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    protected override RetryContext<TContext> CreateRetryContext(Exception exception, CancellationToken cancellationToken)
    {
        return new NoRetryContext<TContext>(Context, exception, cancellationToken);
    }
}

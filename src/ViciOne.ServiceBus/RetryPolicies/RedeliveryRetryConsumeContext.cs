using System;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>
/// Provides a redelivery retry consume context implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class RedeliveryRetryConsumeContext<T> :
    RetryConsumeContext<T>
    where T : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="retryPolicy">The retry policy value.</param>
    /// <param name="retryContext">The retry context value.</param>
    public RedeliveryRetryConsumeContext(ConsumeContext<T> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        : base(context, retryPolicy, retryContext)
    {
    }

    /// <summary>
    /// Creates next.
    /// </summary>
    /// <typeparam name="TContext">The t context type.</typeparam>
    /// <param name="retryContext">The retry context value.</param>
    /// <returns>The result of the operation.</returns>
    public override TContext CreateNext<TContext>(RetryContext retryContext)
    {
        return this as TContext
            ?? throw new ArgumentException($"The context type is not valid: {TypeCache<T>.ShortName}");
    }
}

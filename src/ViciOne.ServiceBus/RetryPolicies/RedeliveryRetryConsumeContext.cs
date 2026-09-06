using System;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Carries state for redelivery retry consume operations.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class RedeliveryRetryConsumeContext<T> :
    RetryConsumeContext<T>
    where T : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="retryPolicy">The retry policy.</param>
    /// <param name="retryContext">The retry context.</param>
    public RedeliveryRetryConsumeContext(ConsumeContext<T> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        : base(context, retryPolicy, retryContext)
    {
    }

    /// <summary>Creates next.</summary>
    /// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
    /// <param name="retryContext">The retry context.</param>
    /// <returns>The created next.</returns>
    public override TContext CreateNext<TContext>(RetryContext retryContext)
    {
        return this as TContext
            ?? throw new ArgumentException($"The context type is not valid: {TypeCache<T>.ShortName}");
    }
}

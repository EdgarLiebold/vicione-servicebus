using System;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Preserves consume state while a retry decision is projected to broker redelivery.</summary>
/// <typeparam name="T">The consumed message type.</typeparam>
internal sealed class RedeliveryRetryConsumeContext<T> :
    RetryConsumeContext<T>
    where T : class
{
    /// <summary>Creates a redelivery retry scope.</summary>
    /// <param name="context">The consumed message context.</param>
    /// <param name="retryPolicy">The policy that determines the redelivery schedule.</param>
    /// <param name="retryContext">The active retry state, or <see langword="null" /> before scheduling.</param>
    public RedeliveryRetryConsumeContext(ConsumeContext<T> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        : base(context, retryPolicy, retryContext)
    {
    }

    /// <summary>Returns the stable consume scope used across broker redeliveries.</summary>
    /// <typeparam name="TContext">The requested consume-retry context contract.</typeparam>
    /// <param name="retryContext">The policy state for the scheduled redelivery.</param>
    /// <returns>This redelivery scope represented as the requested contract.</returns>
    public override TContext CreateNext<TContext>(RetryContext retryContext)
    {
        ArgumentNullException.ThrowIfNull(retryContext);

        return this as TContext
            ?? throw new ArgumentException(
                $"The retry context cannot be represented as {TypeCache<TContext>.ShortName}.", nameof(TContext));
    }
}

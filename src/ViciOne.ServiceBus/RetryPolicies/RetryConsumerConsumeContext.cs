using System;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Adds retry counters and deferred faults to one consumer instance context.</summary>
/// <typeparam name="TConsumer">The consumer type.</typeparam>
internal sealed class RetryConsumerConsumeContext<TConsumer> :
    RetryConsumeContext,
    ConsumerConsumeContext<TConsumer>
    where TConsumer : class
{
    readonly ConsumerConsumeContext<TConsumer> _context;

    /// <summary>Creates a retry scope over one consumer instance context.</summary>
    /// <param name="context">The consumer instance context being retried.</param>
    /// <param name="retryPolicy">The policy that classifies retryable failures.</param>
    /// <param name="retryContext">The active retry state, or <see langword="null" /> before the first retry.</param>
    public RetryConsumerConsumeContext(ConsumerConsumeContext<TConsumer> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        : base(context, retryPolicy, retryContext)
    {
        _context = context;
    }

    /// <summary>Gets the consumer instance.</summary>
    public TConsumer Consumer => _context.Consumer;

    /// <summary>Creates the next consumer retry scope.</summary>
    /// <typeparam name="TContext">The requested consume-retry context contract.</typeparam>
    /// <param name="retryContext">The policy state for the next attempt.</param>
    /// <returns>The next typed consume-retry context.</returns>
    public override TContext CreateNext<TContext>(RetryContext retryContext)
    {
        ArgumentNullException.ThrowIfNull(retryContext);

        return new RetryConsumerConsumeContext<TConsumer>(_context, RetryPolicy, retryContext) as TContext
            ?? throw new ArgumentException(
                $"The retry context cannot be represented as {TypeCache<TContext>.ShortName}.", nameof(TContext));
    }
}

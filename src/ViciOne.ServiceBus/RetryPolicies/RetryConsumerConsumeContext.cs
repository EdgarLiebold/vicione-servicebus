using System;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Carries state for retry consumer consume operations.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public class RetryConsumerConsumeContext<TConsumer> :
    RetryConsumeContext,
    ConsumerConsumeContext<TConsumer>
    where TConsumer : class
{
    readonly ConsumerConsumeContext<TConsumer> _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="retryPolicy">The retry policy.</param>
    /// <param name="retryContext">The retry context.</param>
    public RetryConsumerConsumeContext(ConsumerConsumeContext<TConsumer> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        : base(context, retryPolicy, retryContext)
    {
        _context = context;
    }

    /// <summary>Gets the consumer.</summary>
    public TConsumer Consumer => _context.Consumer;

    /// <summary>Creates next.</summary>
    /// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
    /// <param name="retryContext">The retry context.</param>
    /// <returns>The created next.</returns>
    public override TContext CreateNext<TContext>(RetryContext retryContext)
    {
        return new RetryConsumerConsumeContext<TConsumer>(_context, RetryPolicy, retryContext) as TContext
            ?? throw new ArgumentException($"The context type is not valid: {TypeCache<TContext>.ShortName}");
    }
}

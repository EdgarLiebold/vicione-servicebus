using System;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>
/// Provides a retry consumer consume context implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public class RetryConsumerConsumeContext<TConsumer> :
    RetryConsumeContext,
    ConsumerConsumeContext<TConsumer>
    where TConsumer : class
{
    readonly ConsumerConsumeContext<TConsumer> _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="retryPolicy">The retry policy value.</param>
    /// <param name="retryContext">The retry context value.</param>
    public RetryConsumerConsumeContext(ConsumerConsumeContext<TConsumer> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        : base(context, retryPolicy, retryContext)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the consumer value.
    /// </summary>
    public TConsumer Consumer => _context.Consumer;

    /// <summary>
    /// Creates next.
    /// </summary>
    /// <typeparam name="TContext">The t context type.</typeparam>
    /// <param name="retryContext">The retry context value.</param>
    /// <returns>The result of the operation.</returns>
    public override TContext CreateNext<TContext>(RetryContext retryContext)
    {
        return new RetryConsumerConsumeContext<TConsumer>(_context, RetryPolicy, retryContext) as TContext
            ?? throw new ArgumentException($"The context type is not valid: {TypeCache<TContext>.ShortName}");
    }
}

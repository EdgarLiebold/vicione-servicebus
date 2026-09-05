using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a missing instance redelivery pipe implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MissingInstanceRedeliveryPipe<TSaga, TMessage> :
    IPipe<ConsumeContext<TMessage>>
    where TSaga : SagaStateMachineInstance
    where TMessage : class
{
    readonly IPipe<ConsumeContext<TMessage>> _finalPipe;
    readonly RedeliveryOptions _options;
    readonly IRetryPolicy _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="retryPolicy">The retry policy value.</param>
    /// <param name="finalPipe">The final pipe value.</param>
    /// <param name="options">The options value.</param>
    public MissingInstanceRedeliveryPipe(IRetryPolicy retryPolicy, IPipe<ConsumeContext<TMessage>> finalPipe, RedeliveryOptions options)
    {
        _retryPolicy = retryPolicy;
        _finalPipe = finalPipe;
        _options = options;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(ConsumeContext<TMessage> context)
    {
        using RetryPolicyContext<ConsumeContext<TMessage>> policyContext = _retryPolicy.CreatePolicyContext(context);

        var exception = new SagaException("An existing saga instance was not found", typeof(TSaga), typeof(TMessage), context.CorrelationId ?? Guid.Empty);

        if (!policyContext.CanRetry(exception, out RetryContext<ConsumeContext<TMessage>> retryContext))
            return _finalPipe.SendAsync(context);

        var previousDeliveryCount = context.Advanced().GetRedeliveryCount();
        for (var retryIndex = 0; retryIndex < previousDeliveryCount; retryIndex++)
        {
            if (!retryContext.CanRetry(exception, out retryContext))
                return _finalPipe.SendAsync(context);
        }

        var redeliveryContext = _options.HasFlag(RedeliveryOptions.ConfigureMessageScheduler)
            ? (MessageRedeliveryContext)new ScheduleMessageRedeliveryContext<TMessage>(context, _options)
            : new DelayedMessageRedeliveryContext<TMessage>(context, _options);

        var delay = retryContext.Delay ?? TimeSpan.Zero;

        return redeliveryContext.ScheduleRedeliveryAsync(delay);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
    }
}

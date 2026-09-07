using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Executes the pipeline for missing instance redelivery.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class MissingInstanceRedeliveryPipe<TSaga, TMessage> :
    IPipe<ConsumeContext<TMessage>>
    where TSaga : SagaStateMachineInstance
    where TMessage : class
{
    readonly IPipe<ConsumeContext<TMessage>> _finalPipe;
    readonly RedeliveryOptions _options;
    readonly IRetryPolicy _retryPolicy;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="retryPolicy">The retry policy.</param>
    /// <param name="finalPipe">The final pipe.</param>
    /// <param name="options">The options that control the operation.</param>
    public MissingInstanceRedeliveryPipe(IRetryPolicy retryPolicy, IPipe<ConsumeContext<TMessage>> finalPipe, RedeliveryOptions options)
    {
        _retryPolicy = retryPolicy;
        _finalPipe = finalPipe;
        _options = options;
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
    }
}

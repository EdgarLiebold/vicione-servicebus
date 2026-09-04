using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.RabbitMq.Middleware;

/// <summary>
/// Provides a rabbit mq queue redelivery filter implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public sealed class RabbitMqQueueRedeliveryFilter<TMessage> : IFilter<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly RedeliveryOptions _options;
    readonly RabbitMqQueueRedeliveryPlan _plan;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="plan">The plan value.</param>
    /// <param name="options">The options value.</param>
    public RabbitMqQueueRedeliveryFilter(RabbitMqQueueRedeliveryPlan plan, RedeliveryOptions options)
    {
        _plan = plan;
        _options = options;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("rabbitMqQueueRedelivery");
        scope.Add("messageType", TypeCache<TMessage>.ShortName);
        scope.Add("queue", _plan.QueueName);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    [DebuggerNonUserCode]
    public Task SendAsync(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        context.GetOrAddPayload<MessageRedeliveryContext>(() => new RabbitMqQueueRedeliveryContext<TMessage>(context, _options, _plan));
        return next.SendAsync(context);
    }
}

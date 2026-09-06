using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.RabbitMq.Middleware;

/// <summary>Adds RabbitMQ queue-based redelivery behavior to a consume pipeline.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public sealed class RabbitMqQueueRedeliveryFilter<TMessage> : IFilter<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly RedeliveryOptions _options;
    readonly RabbitMqQueueRedeliveryPlan _plan;

    /// <summary>Creates the filter for a declared redelivery-queue plan.</summary>
    /// <param name="plan">The finite RabbitMQ redelivery intervals and routes.</param>
    /// <param name="options">The provider-neutral redelivery options.</param>
    public RabbitMqQueueRedeliveryFilter(RabbitMqQueueRedeliveryPlan plan, RedeliveryOptions options)
    {
        _plan = plan;
        _options = options;
    }

    /// <summary>Adds the message contract and redelivery queue to the diagnostic probe.</summary>
    /// <param name="context">The probe context that receives the filter scope.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("rabbitMqQueueRedelivery");
        scope.Add("messageType", TypeCache<TMessage>.ShortName);
        scope.Add("queue", _plan.QueueName);
    }

    /// <summary>Adds a queue-redelivery payload and invokes the remaining consume pipeline.</summary>
    /// <param name="context">The consumed message context.</param>
    /// <param name="next">The remainder of the consume pipeline.</param>
    /// <returns>A task that completes with the remaining pipeline.</returns>
    [DebuggerNonUserCode]
    public Task SendAsync(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        context.GetOrAddPayload<MessageRedeliveryContext>(() => new RabbitMqQueueRedeliveryContext<TMessage>(context, _options, _plan));
        return next.SendAsync(context);
    }
}

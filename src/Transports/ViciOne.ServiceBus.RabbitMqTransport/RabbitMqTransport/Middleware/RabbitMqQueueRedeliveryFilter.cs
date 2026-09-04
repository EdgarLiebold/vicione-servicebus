using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.RabbitMqTransport.Middleware;

public sealed class RabbitMqQueueRedeliveryFilter<TMessage> : IFilter<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly RedeliveryOptions _options;
    readonly RabbitMqQueueRedeliveryPlan _plan;

    public RabbitMqQueueRedeliveryFilter(RabbitMqQueueRedeliveryPlan plan, RedeliveryOptions options)
    {
        _plan = plan;
        _options = options;
    }

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("rabbitMqQueueRedelivery");
        scope.Add("messageType", TypeCache<TMessage>.ShortName);
        scope.Add("queue", _plan.QueueName);
    }

    [DebuggerNonUserCode]
    public Task SendAsync(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        context.GetOrAddPayload<MessageRedeliveryContext>(() => new RabbitMqQueueRedeliveryContext<TMessage>(context, _options, _plan));
        return next.SendAsync(context);
    }
}

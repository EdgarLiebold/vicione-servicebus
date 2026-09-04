using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RabbitMqTransport.Middleware;

namespace ViciOne.ServiceBus.RabbitMqTransport.Configuration;

public sealed class RabbitMqQueueRedeliveryPipeSpecification<TMessage> :
    IRedeliveryPipeSpecification,
    IPipeSpecification<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly RabbitMqQueueRedeliveryPlan _plan;

    public RabbitMqQueueRedeliveryPipeSpecification(RabbitMqQueueRedeliveryPlan plan)
    {
        _plan = plan;
    }

    public RedeliveryOptions Options { get; set; }

    public void Apply(IPipeBuilder<ConsumeContext<TMessage>> builder)
    {
        builder.AddFilter(new RabbitMqQueueRedeliveryFilter<TMessage>(_plan, Options));
    }

    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }
}

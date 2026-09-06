using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RabbitMq.Middleware;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Adds RabbitMQ queue-redelivery middleware for one message contract.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public sealed class RabbitMqQueueRedeliveryPipeSpecification<TMessage> :
    IRedeliveryPipeSpecification,
    IPipeSpecification<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly RabbitMqQueueRedeliveryPlan _plan;

    /// <summary>Creates the specification for a finite redelivery-queue plan.</summary>
    /// <param name="plan">The declared RabbitMQ delay queues and routing keys.</param>
    public RabbitMqQueueRedeliveryPipeSpecification(RabbitMqQueueRedeliveryPlan plan)
    {
        _plan = plan;
    }

    /// <summary>Gets or sets provider-neutral redelivery behavior.</summary>
    public RedeliveryOptions Options { get; set; }

    /// <summary>Adds the RabbitMQ queue-redelivery filter to a consume pipeline.</summary>
    /// <param name="builder">The consume-pipeline builder.</param>
    public void Apply(IPipeBuilder<ConsumeContext<TMessage>> builder)
    {
        builder.AddFilter(new RabbitMqQueueRedeliveryFilter<TMessage>(_plan, Options));
    }

    /// <summary>Reports no additional failures because the redelivery plan is validated when it is created.</summary>
    /// <returns>An empty sequence.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }
}

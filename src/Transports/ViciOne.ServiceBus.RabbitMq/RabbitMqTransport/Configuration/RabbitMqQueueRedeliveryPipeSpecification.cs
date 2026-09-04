using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RabbitMq.Middleware;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Provides a rabbit mq queue redelivery pipe specification implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public sealed class RabbitMqQueueRedeliveryPipeSpecification<TMessage> :
    IRedeliveryPipeSpecification,
    IPipeSpecification<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly RabbitMqQueueRedeliveryPlan _plan;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="plan">The plan value.</param>
    public RabbitMqQueueRedeliveryPipeSpecification(RabbitMqQueueRedeliveryPlan plan)
    {
        _plan = plan;
    }

    /// <summary>
    /// Gets or sets the options value.
    /// </summary>
    public RedeliveryOptions Options { get; set; }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<ConsumeContext<TMessage>> builder)
    {
        builder.AddFilter(new RabbitMqQueueRedeliveryFilter<TMessage>(_plan, Options));
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }
}

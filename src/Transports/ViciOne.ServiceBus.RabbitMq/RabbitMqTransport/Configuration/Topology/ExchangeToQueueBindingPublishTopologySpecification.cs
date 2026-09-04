using System.Collections.Generic;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Used to declare an exchange and queue, and bind them together.
/// </summary>
public class ExchangeToQueueBindingPublishTopologySpecification :
    QueueBindingConfigurator,
    IRabbitMqPublishTopologySpecification
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="exchangeName">The exchange name value.</param>
    /// <param name="exchangeType">The exchange type value.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    public ExchangeToQueueBindingPublishTopologySpecification(string exchangeName, string exchangeType, string? queueName = null, bool durable = true,
        bool autoDelete = false)
        : base(queueName ?? exchangeName, exchangeType, durable, autoDelete)
    {
        ExchangeName = exchangeName;
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPublishEndpointBrokerTopologyBuilder builder)
    {
        var exchangeHandle = builder.ExchangeDeclare(ExchangeName, ExchangeType, Durable, AutoDelete, ExchangeArguments);

        var queueHandle = builder.QueueDeclare(QueueName, Durable, AutoDelete, Exclusive, QueueArguments);

        var bindingHandle = builder.QueueBind(exchangeHandle, queueHandle, RoutingKey, BindingArguments);
    }
}

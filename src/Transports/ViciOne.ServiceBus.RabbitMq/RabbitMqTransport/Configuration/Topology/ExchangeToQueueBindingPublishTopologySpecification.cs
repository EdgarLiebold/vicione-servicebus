using System.Collections.Generic;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Declares and binds an auxiliary exchange and queue in publish topology.</summary>
public class ExchangeToQueueBindingPublishTopologySpecification :
    QueueBindingConfigurator,
    IRabbitMqPublishTopologySpecification
{
    /// <summary>Creates auxiliary queue-binding settings.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="exchangeType">The RabbitMQ exchange type.</param>
    /// <param name="queueName">The queue name, or the exchange name when omitted.</param>
    /// <param name="durable">Whether the queue and exchange survive broker restarts.</param>
    /// <param name="autoDelete">Whether RabbitMQ auto-deletes the exchange when unused and the queue after its last consumer is gone.</param>
    public ExchangeToQueueBindingPublishTopologySpecification(string exchangeName, string exchangeType, string? queueName = null, bool durable = true,
        bool autoDelete = false)
        : base(queueName ?? exchangeName, exchangeType, durable, autoDelete)
    {
        ExchangeName = exchangeName;
    }

    /// <summary>Reports no additional validation failures for this declarative topology fragment.</summary>
    /// <returns>An empty sequence.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>Declares the auxiliary exchange and queue and binds the exchange to the queue.</summary>
    /// <param name="builder">The publish-endpoint topology builder.</param>
    public void Apply(IPublishEndpointBrokerTopologyBuilder builder)
    {
        var exchangeHandle = builder.ExchangeDeclare(ExchangeName, ExchangeType, Durable, AutoDelete, ExchangeArguments);

        var queueHandle = builder.QueueDeclare(QueueName, Durable, AutoDelete, Exclusive, QueueArguments);

        var bindingHandle = builder.QueueBind(exchangeHandle, queueHandle, RoutingKey, BindingArguments);
    }
}

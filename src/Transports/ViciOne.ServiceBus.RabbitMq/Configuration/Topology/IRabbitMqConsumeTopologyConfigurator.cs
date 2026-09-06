using System;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures exchanges and bindings deployed for a RabbitMQ receive endpoint.</summary>
public interface IRabbitMqConsumeTopologyConfigurator :
    IConsumeTopologyConfigurator,
    IRabbitMqConsumeTopology
{
    /// <summary>Gets consume topology for a message contract.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <returns>The RabbitMQ message consume topology.</returns>
    new IRabbitMqMessageConsumeTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Adds a broker-topology specification to the endpoint.</summary>
    /// <param name="specification">The specification to apply during topology construction.</param>
    void AddSpecification(IRabbitMqConsumeTopologySpecification specification);

    /// <summary>Binds an exchange to the receive endpoint exchange.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="configure">An optional callback that customizes the exchange and binding.</param>
    void Bind(string exchangeName, Action<IRabbitMqExchangeToExchangeBindingConfigurator>? configure = null);

    /// <summary>
    /// Declares an exchange and queue when necessary, then binds the exchange to the queue.
    /// </summary>
    /// <param name="exchangeName">The exchange name to bind.</param>
    /// <param name="queueName">The queue name to declare/bind to the exchange.</param>
    /// <param name="configure">The configuration callback.</param>
    void BindQueue(string exchangeName, string queueName, Action<IRabbitMqQueueBindingConfigurator>? configure = null);
}

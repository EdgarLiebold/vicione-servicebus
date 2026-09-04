using System;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines the contract for rabbit mq consume topology configurator.
/// </summary>
public interface IRabbitMqConsumeTopologyConfigurator :
    IConsumeTopologyConfigurator,
    IRabbitMqConsumeTopology
{
    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IRabbitMqMessageConsumeTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Adds specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    void AddSpecification(IRabbitMqConsumeTopologySpecification specification);

    /// <summary>
    /// Bind an exchange, using the configurator
    /// </summary>
    /// <param name="exchangeName"></param>
    /// <param name="configure"></param>
    void Bind(string exchangeName, Action<IRabbitMqExchangeToExchangeBindingConfigurator>? configure = null);

    /// <summary>
    /// Bind an exchange to a queue, both of which are declared if they do not exist. Useful
    /// for creating alternate/dead-letter exchanges and queues for messages
    /// </summary>
    /// <param name="exchangeName">The exchange name to bind</param>
    /// <param name="queueName">The queue name to declare/bind to the exchange</param>
    /// <param name="configure">The configuration callback</param>
    void BindQueue(string exchangeName, string queueName, Action<IRabbitMqQueueBindingConfigurator>? configure = null);
}

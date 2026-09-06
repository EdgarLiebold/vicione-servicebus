using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures RabbitMQ publish topology for one message contract.</summary>
/// <typeparam name="TMessage">The message contract.</typeparam>
public interface IRabbitMqMessagePublishTopologyConfigurator<TMessage> :
    IMessagePublishTopologyConfigurator<TMessage>,
    IRabbitMqMessagePublishTopology<TMessage>,
    IRabbitMqMessagePublishTopologyConfigurator
    where TMessage : class
{
}


/// <summary>Configures the exchange and bindings used to publish one message contract.</summary>
public interface IRabbitMqMessagePublishTopologyConfigurator :
    IMessagePublishTopologyConfigurator,
    IRabbitMqExchangeConfigurator
{
    /// <summary>
    /// Specifies the alternate exchange for the published message exchange, which is where messages are sent if no
    /// queues receive the message.
    /// </summary>
    string AlternateExchange { set; }

    /// <summary>Declares an exchange and queue and binds them.</summary>
    /// <param name="exchangeName">The exchange to declare and bind.</param>
    /// <param name="queueName">The queue to declare, or <see langword="null"/> to use the exchange name.</param>
    /// <param name="configure">An optional callback that customizes the queue, exchange, and binding.</param>
    void BindQueue(string exchangeName, string? queueName, Action<IRabbitMqQueueBindingConfigurator>? configure = null);

    /// <summary>Declares an alternate exchange and queue for unroutable published messages.</summary>
    /// <param name="exchangeName">The alternate exchange to declare.</param>
    /// <param name="queueName">The queue to declare, or <see langword="null"/> to use the exchange name.</param>
    /// <param name="configure">An optional callback that customizes the alternate queue, exchange, and binding.</param>
    void BindAlternateExchangeQueue(string exchangeName, string? queueName = null, Action<IRabbitMqQueueBindingConfigurator>? configure = null);
}

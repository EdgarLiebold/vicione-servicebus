using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Exposes RabbitMQ send and publish topology and creates provider-specific destination addresses.</summary>
public interface IRabbitMqBusTopology :
    IBusTopology
{
    /// <summary>Gets the RabbitMQ publish topology.</summary>
    new IRabbitMqPublishTopology PublishTopology { get; }

    /// <summary>Gets the RabbitMQ send topology.</summary>
    new IRabbitMqSendTopology SendTopology { get; }

    /// <summary>Gets publish topology for a message contract.</summary>
    /// <typeparam name="T">The published message contract type.</typeparam>
    /// <returns>The RabbitMQ publish topology for <typeparamref name="T"/>.</returns>
    new IRabbitMqMessagePublishTopology<T> Publish<T>()
        where T : class;

    /// <summary>Gets send topology for a message contract.</summary>
    /// <typeparam name="T">The sent message contract type.</typeparam>
    /// <returns>The RabbitMQ send topology for <typeparamref name="T"/>.</returns>
    new IRabbitMqMessageSendTopology<T> Send<T>()
        where T : class;

    /// <summary>Builds a destination address for a named exchange.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="configure">An optional callback that customizes the exchange settings encoded in the address.</param>
    /// <returns>The RabbitMQ destination address.</returns>
    Uri GetDestinationAddress(string exchangeName, Action<IRabbitMqExchangeConfigurator>? configure = null);

    /// <summary>Builds a destination address for a message contract's exchange.</summary>
    /// <param name="messageType">The message contract type.</param>
    /// <param name="configure">An optional callback that customizes the exchange settings encoded in the address.</param>
    /// <returns>The RabbitMQ destination address.</returns>
    Uri GetDestinationAddress(Type messageType, Action<IRabbitMqExchangeConfigurator>? configure = null);
}

using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes one message contract declared by a consumer and creates its typed connectors.</summary>
public sealed class ConsumerInterfaceType :
    IMessageInterfaceType
{
    readonly Lazy<IMessageConnectorFactory> _consumeConnectorFactory;

    /// <summary>Creates a lazily materialized connector descriptor for one consumer message contract.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="consumerType">The consumer implementation that declares the message contract.</param>
    public ConsumerInterfaceType(Type messageType, Type consumerType)
    {
        MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        ArgumentNullException.ThrowIfNull(consumerType);

        _consumeConnectorFactory = new Lazy<IMessageConnectorFactory>(() => (IMessageConnectorFactory)
            (Activator.CreateInstance(typeof(ConsumeMessageConnectorFactory<,>).MakeGenericType(consumerType,
                messageType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated.")));
    }

    /// <summary>Gets the message contract declared by the consumer.</summary>
    public Type MessageType { get; }

    /// <summary>Gets the connector used with a factory-created consumer.</summary>
    /// <typeparam name="TConsumer">The consumer type requested by the caller.</typeparam>
    /// <returns>The connector typed for the requested consumer.</returns>
    public IConsumerMessageConnector<TConsumer> GetConsumerConnector<TConsumer>()
        where TConsumer : class
    {
        return _consumeConnectorFactory.Value.CreateConsumerConnector<TConsumer>();
    }

    /// <summary>Gets the connector used with an existing consumer instance.</summary>
    /// <typeparam name="TConsumer">The consumer type requested by the caller.</typeparam>
    /// <returns>The connector typed for the requested consumer.</returns>
    public IInstanceMessageConnector<TConsumer> GetInstanceConnector<TConsumer>()
        where TConsumer : class
    {
        return _consumeConnectorFactory.Value.CreateInstanceConnector<TConsumer>();
    }
}

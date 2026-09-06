using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>A standard asynchronous consumer message type, defined by IConsumer.</summary>
public class ConsumerInterfaceType :
    IMessageInterfaceType
{
    readonly Lazy<IMessageConnectorFactory> _consumeConnectorFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    public ConsumerInterfaceType(Type messageType, Type consumerType)
    {
        MessageType = messageType;

        _consumeConnectorFactory = new Lazy<IMessageConnectorFactory>(() => (IMessageConnectorFactory)
            (Activator.CreateInstance(typeof(ConsumeMessageConnectorFactory<,>).MakeGenericType(consumerType,
                messageType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated.")));
    }

    /// <summary>Gets the message type.</summary>
    public Type MessageType { get; }

    /// <summary>Gets consumer connector.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The consumer connector.</returns>
    public IConsumerMessageConnector<T> GetConsumerConnector<T>()
        where T : class
    {
        return _consumeConnectorFactory.Value.CreateConsumerConnector<T>();
    }

    /// <summary>Gets instance connector.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The instance connector.</returns>
    public IInstanceMessageConnector<T> GetInstanceConnector<T>()
        where T : class
    {
        return _consumeConnectorFactory.Value.CreateInstanceConnector<T>();
    }
}

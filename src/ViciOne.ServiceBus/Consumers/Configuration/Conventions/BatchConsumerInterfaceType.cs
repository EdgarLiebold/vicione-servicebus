using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// A batch consumer
/// </summary>
public class BatchConsumerInterfaceType :
    IMessageInterfaceType
{
    readonly Lazy<IMessageConnectorFactory> _consumeConnectorFactory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="batchMessageType">The batch message type value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    public BatchConsumerInterfaceType(Type batchMessageType, Type messageType, Type consumerType)
    {
        MessageType = batchMessageType;

        _consumeConnectorFactory = new Lazy<IMessageConnectorFactory>(() => (IMessageConnectorFactory)
            (Activator.CreateInstance(typeof(BatchMessageConnectorFactory<,>).MakeGenericType(consumerType, messageType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated.")));
    }

    /// <summary>
    /// Gets the message type value.
    /// </summary>
    public Type MessageType { get; }

    /// <summary>
    /// Gets consumer connector.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public IConsumerMessageConnector<T> GetConsumerConnector<T>()
        where T : class
    {
        return _consumeConnectorFactory.Value.CreateConsumerConnector<T>();
    }

    /// <summary>
    /// Gets instance connector.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public IInstanceMessageConnector<T> GetInstanceConnector<T>()
        where T : class
    {
        return _consumeConnectorFactory.Value.CreateInstanceConnector<T>();
    }
}

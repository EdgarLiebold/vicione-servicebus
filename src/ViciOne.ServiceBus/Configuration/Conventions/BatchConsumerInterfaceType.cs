using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes a completed-batch consumer contract and creates its typed batch connector.</summary>
public sealed class BatchConsumerInterfaceType :
    IMessageInterfaceType
{
    readonly Lazy<IMessageConnectorFactory> _consumeConnectorFactory;

    /// <summary>Creates a lazily materialized connector descriptor for one completed-batch contract.</summary>
    /// <param name="batchMessageType">The closed batch contract consumed by the implementation.</param>
    /// <param name="messageType">The individual message contract collected into the batch.</param>
    /// <param name="consumerType">The consumer implementation that receives completed batches.</param>
    public BatchConsumerInterfaceType(Type batchMessageType, Type messageType, Type consumerType)
    {
        MessageType = batchMessageType ?? throw new ArgumentNullException(nameof(batchMessageType));
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(consumerType);

        _consumeConnectorFactory = new Lazy<IMessageConnectorFactory>(() => (IMessageConnectorFactory)
            (Activator.CreateInstance(typeof(BatchMessageConnectorFactory<,>).MakeGenericType(consumerType, messageType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated.")));
    }

    /// <summary>Gets the closed batch contract accepted by the consumer.</summary>
    public Type MessageType { get; }

    /// <summary>Gets the connector used with a factory-created batch consumer.</summary>
    /// <typeparam name="TConsumer">The consumer type requested by the caller.</typeparam>
    /// <returns>The connector typed for the requested consumer.</returns>
    public IConsumerMessageConnector<TConsumer> GetConsumerConnector<TConsumer>()
        where TConsumer : class
    {
        return _consumeConnectorFactory.Value.CreateConsumerConnector<TConsumer>();
    }

    /// <summary>Rejects instance connection because batch consumers are collector-owned.</summary>
    /// <typeparam name="TConsumer">The consumer type requested by the caller.</typeparam>
    /// <returns>This method does not return.</returns>
    public IInstanceMessageConnector<TConsumer> GetInstanceConnector<TConsumer>()
        where TConsumer : class
    {
        return _consumeConnectorFactory.Value.CreateInstanceConnector<TConsumer>();
    }
}

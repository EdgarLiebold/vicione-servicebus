using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides the connector for a discovered batch consumer interface.</summary>
/// <typeparam name="TConsumer">The consumer that receives completed batches.</typeparam>
/// <typeparam name="TMessage">The message contract collected into batches.</typeparam>
internal sealed class BatchMessageConnectorFactory<TConsumer, TMessage> :
    IMessageConnectorFactory
    where TConsumer : class, IConsumer<IMessageBatch<TMessage>>
    where TMessage : class
{
    readonly BatchConsumerMessageConnector<TConsumer, TMessage> _consumerConnector;

    /// <summary>Creates the connector shared by type-compatible factory requests.</summary>
    public BatchMessageConnectorFactory()
    {
        _consumerConnector = new BatchConsumerMessageConnector<TConsumer, TMessage>();
    }

    /// <summary>Returns the batch connector when the requested consumer type matches.</summary>
    /// <typeparam name="TRequestedConsumer">The requested consumer type.</typeparam>
    /// <returns>The connector typed for the requested consumer.</returns>
    public IConsumerMessageConnector<TRequestedConsumer> CreateConsumerConnector<TRequestedConsumer>()
        where TRequestedConsumer : class
    {
        return _consumerConnector as IConsumerMessageConnector<TRequestedConsumer>
            ?? throw new ArgumentException("The consumer type did not match the connector type.");
    }

    /// <summary>Rejects instance-based connection because a batch requires a collector-owned consumer.</summary>
    /// <typeparam name="TRequestedConsumer">The requested consumer-instance type.</typeparam>
    /// <returns>This method does not return.</returns>
    /// <exception cref="NotSupportedException">Always thrown because batch consumers cannot be connected as instances.</exception>
    public IInstanceMessageConnector<TRequestedConsumer> CreateInstanceConnector<TRequestedConsumer>()
        where TRequestedConsumer : class
    {
        throw new NotSupportedException($"{TypeCache<IMessageBatch<TMessage>>.ShortName} cannot be connected to a consumer instance.");
    }
}

using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates batch message connector instances.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class BatchMessageConnectorFactory<TConsumer, TMessage> :
    IMessageConnectorFactory
    where TConsumer : class, IConsumer<Batch<TMessage>>
    where TMessage : class
{
    readonly BatchConsumerMessageConnector<TConsumer, TMessage> _consumerConnector;

    /// <summary>Initializes a new instance.</summary>
    public BatchMessageConnectorFactory()
    {
        _consumerConnector = new BatchConsumerMessageConnector<TConsumer, TMessage>();
    }

    /// <summary>Creates consumer connector.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The created consumer connector.</returns>
    public IConsumerMessageConnector<T> CreateConsumerConnector<T>()
        where T : class
    {
        return _consumerConnector as IConsumerMessageConnector<T> ?? throw new ArgumentException("The consumer type did not match the connector type");
    }

    /// <summary>Creates instance connector.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The created instance connector.</returns>
    public IInstanceMessageConnector<T> CreateInstanceConnector<T>()
        where T : class
    {
        throw new NotSupportedException($"Batch<{TypeCache<TMessage>.ShortName}> cannot be connected to a consumer instance.");
    }
}

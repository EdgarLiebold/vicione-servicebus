using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a batch message connector factory implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class BatchMessageConnectorFactory<TConsumer, TMessage> :
    IMessageConnectorFactory
    where TConsumer : class, IConsumer<Batch<TMessage>>
    where TMessage : class
{
    readonly BatchConsumerMessageConnector<TConsumer, TMessage> _consumerConnector;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public BatchMessageConnectorFactory()
    {
        _consumerConnector = new BatchConsumerMessageConnector<TConsumer, TMessage>();
    }

    /// <summary>
    /// Creates consumer connector.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public IConsumerMessageConnector<T> CreateConsumerConnector<T>()
        where T : class
    {
        return _consumerConnector as IConsumerMessageConnector<T> ?? throw new ArgumentException("The consumer type did not match the connector type");
    }

    /// <summary>
    /// Creates instance connector.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public IInstanceMessageConnector<T> CreateInstanceConnector<T>()
        where T : class
    {
        throw new NotSupportedException($"Batch<{TypeCache<TMessage>.ShortName}> cannot be connected to a consumer instance.");
    }
}

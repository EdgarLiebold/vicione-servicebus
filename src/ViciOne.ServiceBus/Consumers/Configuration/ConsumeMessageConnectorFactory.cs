using System;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a consume message connector factory implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ConsumeMessageConnectorFactory<TConsumer, TMessage> :
    IMessageConnectorFactory
    where TConsumer : class, IConsumer<TMessage>
    where TMessage : class
{
    readonly ConsumerMessageConnector<TConsumer, TMessage> _consumerConnector;
    readonly InstanceMessageConnector<TConsumer, TMessage> _instanceConnector;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ConsumeMessageConnectorFactory()
    {
        var filter = new MethodConsumerMessageFilter<TConsumer, TMessage>();

        _consumerConnector = new ConsumerMessageConnector<TConsumer, TMessage>(filter);
        _instanceConnector = new InstanceMessageConnector<TConsumer, TMessage>(filter);
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

    IInstanceMessageConnector<T> IMessageConnectorFactory.CreateInstanceConnector<T>()
    {
        return _instanceConnector as IInstanceMessageConnector<T> ?? throw new ArgumentException("The consumer type did not match the connector type");
    }
}

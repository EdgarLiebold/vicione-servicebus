using System;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates consume message connector instances.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ConsumeMessageConnectorFactory<TConsumer, TMessage> :
    IMessageConnectorFactory
    where TConsumer : class, IConsumer<TMessage>
    where TMessage : class
{
    readonly ConsumerMessageConnector<TConsumer, TMessage> _consumerConnector;
    readonly InstanceMessageConnector<TConsumer, TMessage> _instanceConnector;

    /// <summary>Initializes a new instance.</summary>
    public ConsumeMessageConnectorFactory()
    {
        var filter = new MethodConsumerMessageFilter<TConsumer, TMessage>();

        _consumerConnector = new ConsumerMessageConnector<TConsumer, TMessage>(filter);
        _instanceConnector = new InstanceMessageConnector<TConsumer, TMessage>(filter);
    }

    /// <summary>Creates consumer connector.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The created consumer connector.</returns>
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

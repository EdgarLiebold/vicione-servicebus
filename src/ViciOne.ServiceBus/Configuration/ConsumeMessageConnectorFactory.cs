using System;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides factory-created and existing-instance connectors for one consumer message contract.</summary>
/// <typeparam name="TConsumer">The consumer implementation represented by the connectors.</typeparam>
/// <typeparam name="TMessage">The message contract consumed by that implementation.</typeparam>
public sealed class ConsumeMessageConnectorFactory<TConsumer, TMessage> :
    IMessageConnectorFactory
    where TConsumer : class, IConsumer<TMessage>
    where TMessage : class
{
    readonly ConsumerMessageConnector<TConsumer, TMessage> _consumerConnector;
    readonly InstanceMessageConnector<TConsumer, TMessage> _instanceConnector;

    /// <summary>Creates matching connectors for factory-created and existing consumer instances.</summary>
    public ConsumeMessageConnectorFactory()
    {
        var filter = new MethodConsumerMessageFilter<TConsumer, TMessage>();

        _consumerConnector = new ConsumerMessageConnector<TConsumer, TMessage>(filter);
        _instanceConnector = new InstanceMessageConnector<TConsumer, TMessage>(filter);
    }

    /// <summary>Gets the connector used with a factory-created consumer.</summary>
    /// <typeparam name="TRequestedConsumer">The consumer type requested by the caller.</typeparam>
    /// <returns>The connector typed for the requested consumer.</returns>
    public IConsumerMessageConnector<TRequestedConsumer> CreateConsumerConnector<TRequestedConsumer>()
        where TRequestedConsumer : class
    {
        return _consumerConnector as IConsumerMessageConnector<TRequestedConsumer>
            ?? throw new ArgumentException("The consumer type did not match the connector type.");
    }

    IInstanceMessageConnector<TRequestedConsumer> IMessageConnectorFactory.CreateInstanceConnector<TRequestedConsumer>()
    {
        return _instanceConnector as IInstanceMessageConnector<TRequestedConsumer>
            ?? throw new ArgumentException("The consumer type did not match the connector type.");
    }
}

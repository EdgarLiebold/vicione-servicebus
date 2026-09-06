using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Declares a destination exchange and binds the published message exchange to it.</summary>
public class ExchangeBindingPublishTopologySpecification :
    RabbitMqExchangeBindingConfigurator,
    IRabbitMqPublishTopologySpecification
{
    /// <summary>Creates a publish binding from explicit destination exchange settings.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="exchangeType">The RabbitMQ exchange type.</param>
    /// <param name="durable">Whether the exchange survives broker restarts.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the exchange when unused.</param>
    public ExchangeBindingPublishTopologySpecification(string exchangeName, string exchangeType, bool durable = true, bool autoDelete = false)
        : base(exchangeName, exchangeType, durable, autoDelete)
    {
    }

    /// <summary>Reports no additional validation failures for this declarative topology fragment.</summary>
    /// <returns>An empty sequence.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>Declares the destination exchange and binds the current publish exchange to it.</summary>
    /// <param name="builder">The publish-endpoint topology builder.</param>
    public void Apply(IPublishEndpointBrokerTopologyBuilder builder)
    {
        var exchangeHandle = builder.ExchangeDeclare(ExchangeName, ExchangeType, Durable, AutoDelete, ExchangeArguments);

        var sourceExchange = builder.Exchange
            ?? throw new InvalidOperationException("A source exchange must be declared before applying an exchange binding.");

        builder.ExchangeBind(sourceExchange, exchangeHandle, RoutingKey, BindingArguments);
    }
}

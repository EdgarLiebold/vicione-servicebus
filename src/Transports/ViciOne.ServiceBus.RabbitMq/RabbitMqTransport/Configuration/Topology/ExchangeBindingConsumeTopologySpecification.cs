using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Declares a message exchange and binds it to the receive endpoint exchange.</summary>
public class ExchangeBindingConsumeTopologySpecification :
    RabbitMqExchangeBindingConfigurator,
    IRabbitMqExchangeToExchangeBindingConfigurator,
    IRabbitMqConsumeTopologySpecification
{
    readonly List<IRabbitMqConsumeTopologySpecification> _specifications;

    /// <summary>Creates a binding specification from explicit exchange settings.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="exchangeType">The RabbitMQ exchange type.</param>
    /// <param name="durable">Whether the exchange survives broker restarts.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the exchange when unused.</param>
    public ExchangeBindingConsumeTopologySpecification(string exchangeName, string exchangeType, bool durable = true, bool autoDelete = false)
        : base(exchangeName, exchangeType, durable, autoDelete)
    {
        _specifications = new List<IRabbitMqConsumeTopologySpecification>();
    }

    /// <summary>Creates a binding specification by copying an exchange declaration.</summary>
    /// <param name="exchange">The exchange settings to copy.</param>
    public ExchangeBindingConsumeTopologySpecification(Exchange exchange)
        : base(exchange)
    {
        _specifications = new List<IRabbitMqConsumeTopologySpecification>();
    }

    /// <summary>Reports no additional validation failures for this declarative topology fragment.</summary>
    /// <returns>An empty sequence.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>Declares the message exchange, binds it to the receive exchange, and applies nested source bindings.</summary>
    /// <param name="builder">The receive-endpoint topology builder.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        var exchangeHandle = builder.ExchangeDeclare(ExchangeName, ExchangeType, Durable, AutoDelete, ExchangeArguments);

        builder.ExchangeBind(exchangeHandle, builder.Exchange, RoutingKey, BindingArguments);

        builder.BoundExchange = exchangeHandle;

        foreach (var specification in _specifications)
            specification.Apply(builder);
    }

    /// <summary>Adds a source exchange that feeds this message exchange.</summary>
    /// <param name="exchangeName">The nested exchange name.</param>
    /// <param name="configure">An optional callback that customizes the nested exchange and binding.</param>
    public void Bind(string exchangeName, Action<IRabbitMqExchangeToExchangeBindingConfigurator>? configure)
    {
        if (string.IsNullOrWhiteSpace(exchangeName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(exchangeName));

        var specification =
            new ExchangeToExchangeBindingConsumeTopologySpecification(exchangeName, ExchangeType, Durable, AutoDelete) { RoutingKey = RoutingKey };

        configure?.Invoke(specification);

        _specifications.Add(specification);
    }
}

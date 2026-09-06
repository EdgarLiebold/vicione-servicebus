using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Declares a nested exchange and binds it to the current consume-topology exchange.</summary>
public class ExchangeToExchangeBindingConsumeTopologySpecification :
    RabbitMqExchangeBindingConfigurator,
    IRabbitMqExchangeToExchangeBindingConfigurator,
    IRabbitMqConsumeTopologySpecification
{
    readonly List<IRabbitMqConsumeTopologySpecification> _specifications;

    /// <summary>Creates a nested consume binding from explicit exchange settings.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="exchangeType">The RabbitMQ exchange type.</param>
    /// <param name="durable">Whether the exchange survives broker restarts.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the exchange when unused.</param>
    public ExchangeToExchangeBindingConsumeTopologySpecification(string exchangeName, string exchangeType, bool durable = true, bool autoDelete = false)
        : base(exchangeName, exchangeType, durable, autoDelete)
    {
        _specifications = new List<IRabbitMqConsumeTopologySpecification>();
    }

    /// <summary>Reports no additional validation failures for this declarative topology fragment.</summary>
    /// <returns>An empty sequence.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>Declares this source exchange, binds it to the current exchange, and applies nested source bindings.</summary>
    /// <param name="builder">The receive-endpoint topology builder whose binding cursor is restored before returning.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        if (builder.BoundExchange == null)
            throw new ArgumentException("The builder should have an already bound exchange", nameof(builder));

        // Nested specifications temporarily replace this cursor and must restore it before returning.
        var boundExchange = builder.BoundExchange;

        var exchangeHandle = builder.ExchangeDeclare(ExchangeName, ExchangeType, Durable, AutoDelete, ExchangeArguments);

        builder.ExchangeBind(exchangeHandle, boundExchange, RoutingKey, BindingArguments);

        builder.BoundExchange = exchangeHandle;

        foreach (var specification in _specifications)
            specification.Apply(builder);

        builder.BoundExchange = boundExchange;
    }

    /// <summary>Adds another source exchange that feeds this exchange.</summary>
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

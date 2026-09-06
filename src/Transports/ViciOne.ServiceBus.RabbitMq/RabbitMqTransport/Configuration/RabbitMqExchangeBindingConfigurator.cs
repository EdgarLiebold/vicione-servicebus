using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Configures a RabbitMQ exchange and the binding that uses it.</summary>
public abstract class RabbitMqExchangeBindingConfigurator :
    RabbitMqExchangeConfigurator,
    IRabbitMqExchangeBindingConfigurator
{
    /// <summary>Creates exchange and binding settings from explicit values.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="exchangeType">The RabbitMQ exchange type.</param>
    /// <param name="durable">Whether the exchange survives broker restarts.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the exchange when unused.</param>
    /// <param name="routingKey">The binding routing key, or an empty key when omitted.</param>
    protected RabbitMqExchangeBindingConfigurator(string exchangeName, string exchangeType, bool durable = true, bool autoDelete = false,
        string? routingKey = null)
        : base(exchangeName, exchangeType, durable, autoDelete)
    {
        RoutingKey = routingKey ?? "";

        BindingArguments = new Dictionary<string, object?>();
    }

    /// <summary>Copies exchange settings and creates an empty binding-argument set.</summary>
    /// <param name="exchange">The exchange settings to copy.</param>
    /// <param name="routingKey">The binding routing key, or an empty key when omitted.</param>
    protected RabbitMqExchangeBindingConfigurator(Exchange exchange, string? routingKey = null)
        : base(exchange)
    {
        RoutingKey = routingKey ?? "";

        BindingArguments = new Dictionary<string, object?>();
    }

    /// <summary>Gets the binding arguments.</summary>
    public IDictionary<string, object?> BindingArguments { get; }

    /// <summary>Sets a binding argument, or removes it when the value is <see langword="null" />.</summary>
    /// <param name="key">The RabbitMQ binding-argument key.</param>
    /// <param name="value">The argument value.</param>
    public void SetBindingArgument(string key, object? value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (value == null)
            BindingArguments.Remove(key);
        else
            BindingArguments[key] = value;
    }

    /// <summary>Gets or sets the routing key.</summary>
    public string RoutingKey { get; set; }
}

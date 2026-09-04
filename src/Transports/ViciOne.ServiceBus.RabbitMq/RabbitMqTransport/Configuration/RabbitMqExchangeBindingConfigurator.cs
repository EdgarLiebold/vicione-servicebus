using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Provides a rabbit mq exchange binding configurator implementation.
/// </summary>
public abstract class RabbitMqExchangeBindingConfigurator :
    RabbitMqExchangeConfigurator,
    IRabbitMqExchangeBindingConfigurator
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="exchangeName">The exchange name value.</param>
    /// <param name="exchangeType">The exchange type value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    /// <param name="routingKey">The routing key value.</param>
    protected RabbitMqExchangeBindingConfigurator(string exchangeName, string exchangeType, bool durable = true, bool autoDelete = false,
        string? routingKey = null)
        : base(exchangeName, exchangeType, durable, autoDelete)
    {
        RoutingKey = routingKey ?? "";

        BindingArguments = new Dictionary<string, object?>();
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="exchange">The exchange value.</param>
    /// <param name="routingKey">The routing key value.</param>
    protected RabbitMqExchangeBindingConfigurator(Exchange exchange, string? routingKey = null)
        : base(exchange)
    {
        RoutingKey = routingKey ?? "";

        BindingArguments = new Dictionary<string, object?>();
    }

    /// <summary>
    /// Gets the binding arguments value.
    /// </summary>
    public IDictionary<string, object?> BindingArguments { get; }

    /// <summary>
    /// Sets binding argument.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    public void SetBindingArgument(string key, object? value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (value == null)
            BindingArguments.Remove(key);
        else
            BindingArguments[key] = value;
    }

    /// <summary>
    /// Gets or sets the routing key value.
    /// </summary>
    public string RoutingKey { get; set; }
}

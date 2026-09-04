using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Provides a queue binding configurator implementation.
/// </summary>
public class QueueBindingConfigurator :
    RabbitMqQueueConfigurator,
    IRabbitMqQueueBindingConfigurator
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="exchangeType">The exchange type value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    protected QueueBindingConfigurator(string queueName, string exchangeType, bool durable, bool autoDelete)
        : base(queueName, exchangeType, durable, autoDelete)
    {
        BindingArguments = new Dictionary<string, object?>();
        RoutingKey = "";
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

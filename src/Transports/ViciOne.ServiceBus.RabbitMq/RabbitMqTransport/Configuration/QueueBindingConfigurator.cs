using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Configures a RabbitMQ queue, its exchange, and their binding arguments.</summary>
public class QueueBindingConfigurator :
    RabbitMqQueueConfigurator,
    IRabbitMqQueueBindingConfigurator
{
    /// <summary>Creates queue-binding topology with an initially empty routing key.</summary>
    /// <param name="queueName">The queue and exchange name.</param>
    /// <param name="exchangeType">The RabbitMQ exchange type.</param>
    /// <param name="durable">Whether the queue and exchange survive broker restarts.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the queue and exchange when unused.</param>
    protected QueueBindingConfigurator(string queueName, string exchangeType, bool durable, bool autoDelete)
        : base(queueName, exchangeType, durable, autoDelete)
    {
        BindingArguments = new Dictionary<string, object?>();
        RoutingKey = "";
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

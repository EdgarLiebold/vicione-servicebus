using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Provides a rabbit mq exchange configurator implementation.
/// </summary>
public class RabbitMqExchangeConfigurator :
    IRabbitMqExchangeConfigurator,
    Exchange
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="exchangeName">The exchange name value.</param>
    /// <param name="exchangeType">The exchange type value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    public RabbitMqExchangeConfigurator(string exchangeName, string exchangeType, bool durable = true, bool autoDelete = false)
    {
        ExchangeName = exchangeName;
        ExchangeType = exchangeType;
        Durable = durable;
        AutoDelete = autoDelete;

        ExchangeArguments = new Dictionary<string, object?>();
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="source">The source value.</param>
    public RabbitMqExchangeConfigurator(Exchange source)
    {
        ExchangeName = source.ExchangeName;
        ExchangeType = source.ExchangeType;
        Durable = source.Durable;
        AutoDelete = source.AutoDelete;

        ExchangeArguments = new Dictionary<string, object?>(source.ExchangeArguments);
    }

    /// <summary>
    /// Gets or sets the exchange name value.
    /// </summary>
    public string ExchangeName { get; set; }

    /// <summary>
    /// Gets the exchange arguments value.
    /// </summary>
    public IDictionary<string, object?> ExchangeArguments { get; }
    /// <summary>
    /// Gets or sets the exchange type value.
    /// </summary>
    public string ExchangeType { get; set; }
    /// <summary>
    /// Gets or sets the durable value.
    /// </summary>
    public bool Durable { get; set; }
    /// <summary>
    /// Gets or sets the auto delete value.
    /// </summary>
    public bool AutoDelete { get; set; }

    /// <summary>
    /// Sets exchange argument.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    public void SetExchangeArgument(string key, object? value)
    {
        if (value != null)
            ExchangeArguments[key] = value;
        else
            ExchangeArguments.Remove(key);
    }

    /// <summary>
    /// Sets exchange argument.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    public void SetExchangeArgument(string key, TimeSpan value)
    {
        var milliseconds = (int)value.TotalMilliseconds;

        SetExchangeArgument(key, milliseconds);
    }

    /// <summary>
    /// Gets endpoint address.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <returns>The result of the operation.</returns>
    public virtual RabbitMqEndpointAddress GetEndpointAddress(Uri hostAddress)
    {
        return new RabbitMqEndpointAddress(hostAddress, ExchangeName, ExchangeType, Durable, AutoDelete,
            delayedType: ExchangeArguments.TryGetValue("x-delayed-type", out var argument) && argument is string delayedType ? delayedType : default,
            alternateExchange: ExchangeArguments.TryGetValue(RabbitMQ.Client.Headers.AlternateExchange, out argument)
                && argument is string alternateExchange ? alternateExchange : default);
    }
}

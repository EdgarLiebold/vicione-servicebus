using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Stores a RabbitMQ exchange declaration and its arguments.</summary>
public class RabbitMqExchangeConfigurator :
    IRabbitMqExchangeConfigurator,
    Exchange
{
    /// <summary>Creates exchange settings from explicit declaration values.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="exchangeType">The RabbitMQ exchange type.</param>
    /// <param name="durable">Whether the exchange survives broker restarts.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the exchange when unused.</param>
    public RabbitMqExchangeConfigurator(string exchangeName, string exchangeType, bool durable = true, bool autoDelete = false)
    {
        ExchangeName = exchangeName;
        ExchangeType = exchangeType;
        Durable = durable;
        AutoDelete = autoDelete;

        ExchangeArguments = new Dictionary<string, object?>();
    }

    /// <summary>Copies an exchange declaration and its argument snapshot.</summary>
    /// <param name="source">The exchange settings to copy.</param>
    public RabbitMqExchangeConfigurator(Exchange source)
    {
        ExchangeName = source.ExchangeName;
        ExchangeType = source.ExchangeType;
        Durable = source.Durable;
        AutoDelete = source.AutoDelete;

        ExchangeArguments = new Dictionary<string, object?>(source.ExchangeArguments);
    }

    /// <summary>Gets or sets the exchange name.</summary>
    public string ExchangeName { get; set; }

    /// <summary>Gets the exchange arguments.</summary>
    public IDictionary<string, object?> ExchangeArguments { get; }
    /// <summary>Gets or sets the exchange type.</summary>
    public string ExchangeType { get; set; }
    /// <summary>Gets or sets whether the exchange survives broker restarts.</summary>
    public bool Durable { get; set; }
    /// <summary>Gets or sets whether RabbitMQ deletes the exchange when unused.</summary>
    public bool AutoDelete { get; set; }

    /// <summary>Sets an exchange argument, or removes it when the value is <see langword="null" />.</summary>
    /// <param name="key">The RabbitMQ exchange-argument key.</param>
    /// <param name="value">The argument value.</param>
    public void SetExchangeArgument(string key, object? value)
    {
        if (value != null)
            ExchangeArguments[key] = value;
        else
            ExchangeArguments.Remove(key);
    }

    /// <summary>Sets an exchange argument from a nonnegative duration converted to whole milliseconds.</summary>
    /// <param name="key">The RabbitMQ exchange-argument key.</param>
    /// <param name="value">The duration to convert.</param>
    public void SetExchangeArgument(string key, TimeSpan value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        SetExchangeArgument(key, RabbitMqDurationArgument.ToMilliseconds(value));
    }

    /// <summary>Creates an endpoint address from the exchange declaration.</summary>
    /// <param name="hostAddress">The RabbitMQ host and virtual-host address.</param>
    /// <returns>The normalized exchange endpoint address.</returns>
    public virtual RabbitMqEndpointAddress GetEndpointAddress(Uri hostAddress)
    {
        return new RabbitMqEndpointAddress(hostAddress, ExchangeName, ExchangeType, Durable, AutoDelete,
            delayedType: ExchangeArguments.TryGetValue("x-delayed-type", out var argument) && argument is string delayedType ? delayedType : default,
            alternateExchange: ExchangeArguments.TryGetValue(RabbitMQ.Client.Headers.AlternateExchange, out argument)
                && argument is string alternateExchange ? alternateExchange : default);
    }
}

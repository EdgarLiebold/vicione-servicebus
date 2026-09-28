using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures an exchange for RabbitMQ.</summary>
public interface IRabbitMqExchangeConfigurator
{
    /// <summary>Specifies whether the exchange survives broker restarts.</summary>
    /// <value><see langword="true" /> for a durable exchange; otherwise, <see langword="false" />.</value>
    bool Durable { set; }

    /// <summary>Specifies whether RabbitMQ deletes the exchange when its last binding disappears.</summary>
    bool AutoDelete { set; }

    /// <summary>Specify the exchange type for the endpoint.</summary>
    string ExchangeType { set; }

    /// <summary>Sets or removes an argument passed to RabbitMQ when declaring the exchange.</summary>
    /// <param name="key">The argument key.</param>
    /// <param name="value">The argument value.</param>
    void SetExchangeArgument(string key, object? value);

    /// <summary>Sets an exchange argument from a nonnegative duration converted to whole milliseconds.</summary>
    /// <param name="key">The RabbitMQ exchange-argument key.</param>
    /// <param name="value">The duration to convert to whole milliseconds.</param>
    void SetExchangeArgument(string key, TimeSpan value);
}

using System.Collections.Generic;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Describes an exchange declaration in a RabbitMQ broker topology.</summary>
public interface Exchange
{
    /// <summary>Gets the exchange name.</summary>
    string ExchangeName { get; }

    /// <summary>Gets the RabbitMQ exchange type.</summary>
    string ExchangeType { get; }

    /// <summary>Gets whether the exchange survives broker restarts.</summary>
    bool Durable { get; }

    /// <summary>Gets whether RabbitMQ deletes the exchange when it is no longer used.</summary>
    bool AutoDelete { get; }

    /// <summary>Gets the broker-specific exchange declaration arguments.</summary>
    IDictionary<string, object?> ExchangeArguments { get; }
}

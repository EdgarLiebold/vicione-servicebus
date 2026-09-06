using System.Collections.Generic;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Describes an exchange-to-exchange binding declaration in a RabbitMQ broker topology.</summary>
public interface ExchangeToExchangeBinding
{
    /// <summary>Gets the source exchange.</summary>
    Exchange Source { get; }

    /// <summary>Gets the destination exchange.</summary>
    Exchange Destination { get; }

    /// <summary>Gets the routing key used by the binding.</summary>
    string RoutingKey { get; }

    /// <summary>Gets the broker-specific binding arguments.</summary>
    IDictionary<string, object?> Arguments { get; }
}

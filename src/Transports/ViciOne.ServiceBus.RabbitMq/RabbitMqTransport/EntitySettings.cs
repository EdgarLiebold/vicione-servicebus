using System.Collections.Generic;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Defines the RabbitMQ exchange declaration shared by send and fault entities.</summary>
public interface EntitySettings
{
    /// <summary>Gets whether the broker entity survives RabbitMQ restarts.</summary>
    bool Durable { get; }

    /// <summary>Gets whether RabbitMQ deletes the broker entity when unused.</summary>
    bool AutoDelete { get; }

    /// <summary>Arguments passed to exchange-declare.</summary>
    IDictionary<string, object?> ExchangeArguments { get; }

    /// <summary>The exchange name to bind to the queue as the default exchange.</summary>
    string ExchangeName { get; }

    /// <summary>The RabbitMQ exchange type.</summary>
    string ExchangeType { get; }
}

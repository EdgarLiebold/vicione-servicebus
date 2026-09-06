using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Defines RabbitMQ queue, exchange, binding, and consumer settings for a receive transport.</summary>
public interface ReceiveSettings :
    EntitySettings
{
    /// <summary>The queue name to receive from.</summary>
    string QueueName { get; }

    /// <summary>The maximum number of unacknowledged deliveries permitted per consumer.</summary>
    ushort PrefetchCount { get; }

    /// <summary>Gets whether the queue belongs exclusively to its declaring connection.</summary>
    bool Exclusive { get; }

    /// <summary>Gets arguments passed to the RabbitMQ queue declaration.</summary>
    IDictionary<string, object?> QueueArguments { get; }

    /// <summary>Gets the routing key.</summary>
    string RoutingKey { get; }

    /// <summary>Gets the binding arguments.</summary>
    IDictionary<string, object?> BindingArguments { get; }

    /// <summary>
    /// Gets whether existing messages are purged when the endpoint first starts. Channel reconnection
    /// does not purge the queue again.
    /// </summary>
    bool PurgeOnStartup { get; }

    /// <summary>Gets arguments passed when starting the RabbitMQ consumer.</summary>
    IDictionary<string, object?> ConsumeArguments { get; }

    /// <summary>Gets whether the broker permits only this consumer on the queue.</summary>
    bool ExclusiveConsumer { get; }

    /// <summary>Gets how long an unused queue may remain before RabbitMQ deletes it.</summary>
    TimeSpan? QueueExpiration { get; }

    /// <summary>Gets whether deployment includes the queue and its exchange binding.</summary>
    bool BindQueue { get; }

    /// <summary>Gets whether RabbitMQ should consider deliveries acknowledged immediately.</summary>
    bool NoAck { get; }

    /// <summary>Gets the explicit consumer tag, or an empty string for a broker-generated tag.</summary>
    string ConsumerTag { get; }

    /// <summary>Creates the input address for these receive topology settings.</summary>
    /// <param name="hostAddress">The RabbitMQ host and virtual-host address.</param>
    /// <returns>The full receive endpoint address.</returns>
    Uri GetInputAddress(Uri hostAddress);
}

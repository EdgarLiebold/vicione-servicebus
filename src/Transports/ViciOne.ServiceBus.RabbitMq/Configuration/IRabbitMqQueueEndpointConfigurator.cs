namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures queue and consumer behavior for a RabbitMQ receive endpoint.</summary>
public interface IRabbitMqQueueEndpointConfigurator :
    IRabbitMqQueueConfigurator
{
    /// <summary>
    /// Purges messages from an existing queue when the receive endpoint starts. Reconnecting an already
    /// started endpoint does not purge the queue again.
    /// </summary>
    bool PurgeOnStartup { set; }

    /// <summary>Sets the priority of the consumer (optional, no default value specified).</summary>
    int ConsumerPriority { set; }

    /// <summary>Specifies whether the broker permits only this consumer on the queue.</summary>
    bool ExclusiveConsumer { set; }
}

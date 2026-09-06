namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Configures queue lifetime, polling, purge, attributes, and tags for an Amazon SQS endpoint.</summary>
public interface IAmazonSqsQueueEndpointConfigurator :
    IAmazonSqsQueueConfigurator
{
    /// <summary>Sets the Amazon SQS long-poll wait time, in seconds.</summary>
    ushort WaitTimeSeconds { set; }

    /// <summary>Sets whether available messages are purged when the endpoint starts.</summary>
    bool PurgeOnStartup { set; }
}

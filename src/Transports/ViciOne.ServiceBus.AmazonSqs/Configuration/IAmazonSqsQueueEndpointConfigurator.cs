namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for amazon sqs queue endpoint configurator.
/// </summary>
public interface IAmazonSqsQueueEndpointConfigurator :
    IAmazonSqsQueueConfigurator
{
    /// <summary>
    /// Gets or sets the wait time seconds value.
    /// </summary>
    ushort WaitTimeSeconds { set; }

    /// <summary>
    /// Gets or sets the purge on startup value.
    /// </summary>
    bool PurgeOnStartup { set; }
}

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for service bus bus topology.
/// </summary>
public interface IServiceBusBusTopology :
    IBusTopology
{
    /// <summary>
    /// Gets the publish topology value.
    /// </summary>
    new IServiceBusPublishTopology PublishTopology { get; }

    /// <summary>
    /// Gets the send topology value.
    /// </summary>
    new IServiceBusSendTopology SendTopology { get; }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IServiceBusMessagePublishTopology<T> Publish<T>()
        where T : class;

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IServiceBusMessageSendTopology<T> Send<T>()
        where T : class;
}

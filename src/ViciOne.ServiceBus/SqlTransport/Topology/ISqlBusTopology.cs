namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for sql bus topology.
/// </summary>
public interface ISqlBusTopology :
    IBusTopology
{
    /// <summary>
    /// Gets the publish topology value.
    /// </summary>
    new ISqlPublishTopology PublishTopology { get; }

    /// <summary>
    /// Gets the send topology value.
    /// </summary>
    new ISqlSendTopology SendTopology { get; }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new ISqlMessagePublishTopology<T> Publish<T>()
        where T : class;

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new ISqlMessageSendTopology<T> Send<T>()
        where T : class;
}

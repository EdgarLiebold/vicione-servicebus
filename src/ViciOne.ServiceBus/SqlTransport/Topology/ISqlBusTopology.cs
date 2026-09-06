namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Defines the operations required by sql bus topology.</summary>
public interface ISqlBusTopology :
    IBusTopology
{
    /// <summary>Gets the publish topology.</summary>
    new ISqlPublishTopology PublishTopology { get; }

    /// <summary>Gets the send topology.</summary>
    new ISqlSendTopology SendTopology { get; }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The sql message publish topology produced by the operation.</returns>
    new ISqlMessagePublishTopology<T> Publish<T>()
        where T : class;

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The sql message send topology produced by the operation.</returns>
    new ISqlMessageSendTopology<T> Send<T>()
        where T : class;
}

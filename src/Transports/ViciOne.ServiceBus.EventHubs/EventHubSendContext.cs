namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Provides Event Hubs routing metadata for an outgoing message.</summary>
public interface EventHubSendContext :
    SendContext,
    PartitionKeySendContext
{
    /// <summary>Gets or sets the target partition identifier.</summary>
    string? PartitionId { get; set; }
}


/// <summary>Provides message data and Event Hubs routing metadata for an outgoing message.</summary>
/// <typeparam name="T">The message type.</typeparam>
public interface EventHubSendContext<out T> :
    SendContext<T>,
    EventHubSendContext
    where T : class
{
}

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Defines the contract for event hub send context.
/// </summary>
public interface EventHubSendContext :
    SendContext,
    PartitionKeySendContext
{
    /// <summary>
    /// Gets or sets the partition id value.
    /// </summary>
    string? PartitionId { get; set; }
}


/// <summary>
/// Defines the contract for event hub send context.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface EventHubSendContext<out T> :
    SendContext<T>,
    EventHubSendContext
    where T : class
{
}

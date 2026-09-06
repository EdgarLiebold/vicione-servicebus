using System.Threading;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Stores a typed outbound message and Event Hubs partition routing metadata.</summary>
/// <typeparam name="T">The message type.</typeparam>
public class EventHubMessageSendContext<T> :
    MessageSendContext<T>,
    EventHubSendContext<T>
    where T : class
{
    /// <summary>Creates an Event Hubs send context for a message.</summary>
    /// <param name="message">The outbound message.</param>
    /// <param name="cancellationToken">Cancels serialization and production.</param>
    public EventHubMessageSendContext(T message, CancellationToken cancellationToken)
        : base(message, cancellationToken)
    {
    }

    /// <summary>Gets or sets the target partition identifier.</summary>
    public string? PartitionId { get; set; }
    /// <summary>Gets or sets the partition key used by Event Hubs routing.</summary>
    public string? PartitionKey { get; set; }
}

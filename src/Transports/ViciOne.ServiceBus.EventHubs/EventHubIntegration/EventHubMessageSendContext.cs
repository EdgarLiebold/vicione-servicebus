using System.Threading;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides an event hub message send context implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class EventHubMessageSendContext<T> :
    MessageSendContext<T>,
    EventHubSendContext<T>
    where T : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public EventHubMessageSendContext(T message, CancellationToken cancellationToken)
        : base(message, cancellationToken)
    {
    }

    /// <summary>
    /// Gets or sets the partition id value.
    /// </summary>
    public string? PartitionId { get; set; }
    /// <summary>
    /// Gets or sets the partition key value.
    /// </summary>
    public string? PartitionKey { get; set; }
}

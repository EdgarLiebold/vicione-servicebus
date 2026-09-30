using System.Threading;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Stores a typed outbound message and Event Hubs partition routing metadata.</summary>
/// <typeparam name="T">The message type.</typeparam>
public class EventHubMessageSendContext<T> :
    MessageSendContext<T>,
    EventHubSendContext<T>,
    ITransportSendMetadata
    where T : class
{
    /// <summary>Creates an Event Hubs send context for a message.</summary>
    /// <param name="message">The outbound message.</param>
    /// <param name="cancellationToken">Cancels serialization and production.</param>
    public EventHubMessageSendContext(T message, CancellationToken cancellationToken)
        : base(message, cancellationToken)
    {
        ConversationId = NewId.NextGuid();
    }

    // Set only after the provider confirms the batch containing this context.
    internal bool IsProviderConfirmed { get; set; }

    /// <summary>Gets or sets the target partition identifier.</summary>
    public string? PartitionId { get; set; }
    /// <summary>Gets or sets the partition key used by Event Hubs routing.</summary>
    public string? PartitionKey { get; set; }

    object ITransportSendMetadata.CaptureNativeMetadata() => new NativeMetadata(PartitionId, PartitionKey);

    string? ITransportSendMetadata.ChangedNativeField(object snapshot)
    {
        var expected = (NativeMetadata)snapshot;
        if (!string.Equals(PartitionId, expected.PartitionId, StringComparison.Ordinal)) return nameof(PartitionId);
        if (!string.Equals(PartitionKey, expected.PartitionKey, StringComparison.Ordinal)) return nameof(PartitionKey);
        return null;
    }

    void ITransportSendMetadata.RestoreNativeMetadata(object snapshot)
    {
        var expected = (NativeMetadata)snapshot;
        PartitionId = expected.PartitionId;
        PartitionKey = expected.PartitionKey;
    }

    private readonly record struct NativeMetadata(string? PartitionId, string? PartitionKey);
}

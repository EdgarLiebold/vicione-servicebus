namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes state for partition key send operations.</summary>
public interface PartitionKeySendContext
{
    /// <summary>The partition key for the message (defaults to "").</summary>
    string? PartitionKey { get; set; }
}

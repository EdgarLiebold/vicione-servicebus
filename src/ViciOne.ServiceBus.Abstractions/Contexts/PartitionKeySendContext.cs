namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for partition key send context.
/// </summary>
public interface PartitionKeySendContext
{
    /// <summary>
    /// The partition key for the message (defaults to "")
    /// </summary>
    string? PartitionKey { get; set; }
}

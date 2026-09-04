namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for partition key consume context.
/// </summary>
public interface PartitionKeyConsumeContext
{
    /// <summary>
    /// The partition key for the message (defaults to "")
    /// </summary>
    string? PartitionKey { get; }
}

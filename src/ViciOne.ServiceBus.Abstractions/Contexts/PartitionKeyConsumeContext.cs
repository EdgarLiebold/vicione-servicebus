namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes state for partition key consume operations.</summary>
public interface PartitionKeyConsumeContext
{
    /// <summary>The partition key for the message (defaults to "").</summary>
    string? PartitionKey { get; }
}

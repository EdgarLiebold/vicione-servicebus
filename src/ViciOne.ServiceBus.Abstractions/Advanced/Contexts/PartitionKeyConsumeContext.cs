namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes the transport partition key associated with a received message.</summary>
public interface PartitionKeyConsumeContext
{
    /// <summary>Gets the partition key, or <see langword="null" /> when none was assigned.</summary>
    string? PartitionKey { get; }
}

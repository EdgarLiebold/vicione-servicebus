namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides transport partition-key metadata for an outgoing message.</summary>
public interface PartitionKeySendContext
{
    /// <summary>Gets or sets the partition key, or <see langword="null" /> when none is assigned.</summary>
    string? PartitionKey { get; set; }
}

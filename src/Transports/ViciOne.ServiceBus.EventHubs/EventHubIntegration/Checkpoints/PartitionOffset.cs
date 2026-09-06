using Azure.Messaging.EventHubs.Processor;

namespace ViciOne.ServiceBus.EventHubs.Checkpoints;

/// <summary>Identifies an event by its Event Hubs partition and provider-defined offset.</summary>
public readonly struct PartitionOffset
{
    readonly string _partitionId;
    readonly string _offsetString;

    PartitionOffset(string partitionId, string offsetString)
    {
        _partitionId = partitionId;
        _offsetString = offsetString;
    }

    /// <summary>Returns the partition identifier and offset separated by a slash.</summary>
    /// <returns>The composite partition-offset key.</returns>
    public override string ToString()
    {
        return $"{_partitionId}/{_offsetString}";
    }

    /// <summary>Creates a partition-offset key for a processed event.</summary>
    /// <param name="args">The Azure SDK event-processing arguments.</param>
    /// <returns>The event's partition and offset.</returns>
    public static implicit operator PartitionOffset(in ProcessEventArgs args)
    {
        return new PartitionOffset(args.Partition.PartitionId, args.Data.OffsetString);
    }
}

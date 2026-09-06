using Azure.Messaging.EventHubs.Processor;

namespace ViciOne.ServiceBus.EventHubs.Checkpoints;

/// <summary>
/// Represents a partition offset value.
/// </summary>
public readonly struct PartitionOffset
{
    readonly string _partitionId;
    readonly string _offsetString;

    PartitionOffset(string partitionId, string offsetString)
    {
        _partitionId = partitionId;
        _offsetString = offsetString;
    }

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override string ToString()
    {
        return $"{_partitionId}/{_offsetString}";
    }

    /// <summary>
    /// Converts a value to <see cref="PartitionOffset" />.
    /// </summary>
    /// <param name="args">The args value.</param>
    /// <returns>The result of the operation.</returns>
    public static implicit operator PartitionOffset(in ProcessEventArgs args)
    {
        return new PartitionOffset(args.Partition.PartitionId, args.Data.OffsetString);
    }
}

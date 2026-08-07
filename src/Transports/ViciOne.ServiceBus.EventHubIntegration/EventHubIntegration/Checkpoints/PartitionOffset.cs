// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EventHubIntegration.Checkpoints
{
    using Azure.Messaging.EventHubs.Processor;


    // ReSharper disable NotAccessedField.Local
    public readonly struct PartitionOffset
    {
        readonly string _partitionId;
        readonly string _offsetString;

        PartitionOffset(string partitionId, string offsetString)
        {
            _partitionId = partitionId;
            _offsetString = offsetString;
        }

        public override string ToString()
        {
            return $"{_partitionId}/{_offsetString}";
        }

        public static implicit operator PartitionOffset(in ProcessEventArgs args)
        {
            return new PartitionOffset(args.Partition.PartitionId, args.Data.OffsetString);
        }
    }
}

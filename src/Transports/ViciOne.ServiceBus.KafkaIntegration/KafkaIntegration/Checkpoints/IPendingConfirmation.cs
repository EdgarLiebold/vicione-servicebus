// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration.Checkpoints
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Confluent.Kafka;


    public interface IPendingConfirmation
    {
        TopicPartition Partition { get; }
        Offset Offset { get; }

        Task Confirmed { get; }

        void Complete();
        void Faulted(Exception exception);
        void Faulted(string message);
        void Canceled(CancellationToken cancellationToken);
    }
}

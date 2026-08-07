// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Confluent.Kafka;


    public interface IConsumerLockContext :
        IAsyncDisposable
    {
        Task Pending(ConsumeResult<byte[], byte[]> result);
        Task Complete(ConsumeResult<byte[], byte[]> result);
        Task Faulted(ConsumeResult<byte[], byte[]> result, Exception exception);
        void Canceled(ConsumeResult<byte[], byte[]> result, CancellationToken cancellationToken);
    }
}

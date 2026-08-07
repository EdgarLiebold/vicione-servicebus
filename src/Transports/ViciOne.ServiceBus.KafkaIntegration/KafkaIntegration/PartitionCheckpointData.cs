// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration
{
    using System.Threading;
    using System.Threading.Tasks;
    using Checkpoints;
    using Confluent.Kafka;


    public class PartitionCheckpointData
    {
        readonly CancellationTokenSource _cancellationTokenSource;
        readonly ICheckpointer _checkpointer;
        readonly PendingConfirmationCollection _pending;

        public PartitionCheckpointData(IConsumer<byte[], byte[]> consumer, ReceiveSettings settings, PendingConfirmationCollection pending)
        {
            _pending = pending;
            _cancellationTokenSource = new CancellationTokenSource();

            _checkpointer = new BatchCheckpointer(consumer, settings, _cancellationTokenSource.Token);
        }

        public Task Pending(ConsumeResult<byte[], byte[]> result)
        {
            var pendingConfirmation = _pending.Add(result.TopicPartitionOffset);
            return _checkpointer.Pending(pendingConfirmation);
        }

        public Task Lost()
        {
            _cancellationTokenSource.Cancel();
            return Close();
        }

        public async Task Close()
        {
            await _checkpointer.DisposeAsync().ConfigureAwait(false);

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }
}

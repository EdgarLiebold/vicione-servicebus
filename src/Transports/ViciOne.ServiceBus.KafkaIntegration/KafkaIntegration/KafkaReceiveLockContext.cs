// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration
{
    using System;
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using Transports;


    public class KafkaReceiveLockContext :
        ReceiveLockContext
    {
        readonly IConsumerLockContext _lockContext;
        readonly ConsumeResult<byte[], byte[]> _result;

        public KafkaReceiveLockContext(ConsumeResult<byte[], byte[]> result, IConsumerLockContext lockContext)
        {
            _result = result;
            _lockContext = lockContext;
        }

        public Task Complete()
        {
            return _lockContext.Complete(_result);
        }

        public Task Faulted(Exception exception)
        {
            return _lockContext.Faulted(_result, exception);
        }

        public Task ValidateLockStatus()
        {
            return Task.CompletedTask;
        }
    }
}

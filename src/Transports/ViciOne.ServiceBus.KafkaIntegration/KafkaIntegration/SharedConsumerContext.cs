// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration
{
    using System;
    using System.Threading;
    using Confluent.Kafka;
    using Logging;
    using ViciOne.ServiceBus.Middleware;


    public class SharedConsumerContext :
        ProxyPipeContext,
        ConsumerContext
    {
        readonly ConsumerContext _context;

        public SharedConsumerContext(ConsumerContext context, CancellationToken cancellationToken)
            : base(context)
        {
            _context = context;
            CancellationToken = cancellationToken;
        }

        public override CancellationToken CancellationToken { get; }

        public ILogContext LogContext => _context.LogContext;

        public IConsumer<byte[], byte[]> CreateConsumer(KafkaConsumerBuilderContext context, Action<IConsumer<byte[], byte[]>, Error> onError,
            int consumerIndex)
        {
            return _context.CreateConsumer(context, onError, consumerIndex);
        }
    }
}

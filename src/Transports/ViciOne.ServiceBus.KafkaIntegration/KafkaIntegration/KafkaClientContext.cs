// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration
{
    using System.Threading;
    using Confluent.Kafka;
    using ViciOne.ServiceBus.Middleware;


    public class KafkaClientContext :
        BasePipeContext,
        ClientContext
    {
        public KafkaClientContext(ClientConfig config, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            Config = config;
        }

        public ClientConfig Config { get; }
    }
}

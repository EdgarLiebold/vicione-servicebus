// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration
{
    using ViciOne.ServiceBus.Configuration;
    using Transports;


    public class ConsumerContextSupervisor :
        TransportPipeContextSupervisor<ConsumerContext>,
        IConsumerContextSupervisor
    {
        public ConsumerContextSupervisor(IHostConfiguration hostConfiguration, IClientContextSupervisor clientContextSupervisor,
            ConsumerBuilderFactory consumerBuilderFactory)
            : base(new ConsumerContextFactory(hostConfiguration, clientContextSupervisor, consumerBuilderFactory))
        {
            clientContextSupervisor.AddConsumeAgent(this);
        }
    }
}

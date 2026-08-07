// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EventHubIntegration
{
    using Transports;


    public class ProducerContextSupervisor :
        TransportPipeContextSupervisor<ProducerContext>,
        IProducerContextSupervisor
    {
        public ProducerContextSupervisor(IConnectionContextSupervisor contextSupervisor, string eventHubName)
            : base(new ProducerContextFactory(contextSupervisor, eventHubName))
        {
            contextSupervisor.AddSendAgent(this);
        }
    }
}

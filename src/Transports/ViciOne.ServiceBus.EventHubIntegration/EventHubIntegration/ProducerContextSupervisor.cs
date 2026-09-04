using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubIntegration;

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

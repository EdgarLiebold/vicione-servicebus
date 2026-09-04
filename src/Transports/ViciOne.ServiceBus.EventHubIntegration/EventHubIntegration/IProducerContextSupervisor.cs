using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubIntegration;

public interface IProducerContextSupervisor :
    ITransportSupervisor<ProducerContext>
{
}

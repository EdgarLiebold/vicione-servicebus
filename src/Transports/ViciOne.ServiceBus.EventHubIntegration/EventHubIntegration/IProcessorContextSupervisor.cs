using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubIntegration;

public interface IProcessorContextSupervisor :
    ITransportSupervisor<ProcessorContext>
{
}

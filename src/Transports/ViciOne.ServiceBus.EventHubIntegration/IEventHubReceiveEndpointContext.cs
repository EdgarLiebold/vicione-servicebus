using ViciOne.ServiceBus.EventHubIntegration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus;

public interface IEventHubReceiveEndpointContext :
    ReceiveEndpointContext
{
    IProcessorContextSupervisor ContextSupervisor { get; }
}

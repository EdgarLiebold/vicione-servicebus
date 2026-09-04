using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubIntegration;

public interface IEventHubDataReceiver :
    IAgent,
    DeliveryMetrics
{
}

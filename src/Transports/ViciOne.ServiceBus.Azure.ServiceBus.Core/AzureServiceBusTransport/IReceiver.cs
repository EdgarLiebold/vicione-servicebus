using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public interface IReceiver :
    IAgent,
    DeliveryMetrics
{
    void Start();
}

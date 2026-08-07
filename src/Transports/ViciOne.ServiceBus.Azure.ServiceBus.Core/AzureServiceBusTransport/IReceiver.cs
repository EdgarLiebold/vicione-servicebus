// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureServiceBusTransport
{
    using Transports;


    public interface IReceiver :
        IAgent,
        DeliveryMetrics
    {
        void Start();
    }
}

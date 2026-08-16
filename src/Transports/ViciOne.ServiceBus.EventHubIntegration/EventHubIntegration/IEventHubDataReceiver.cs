namespace ViciOne.ServiceBus.EventHubIntegration
{
    using Transports;


    public interface IEventHubDataReceiver :
        IAgent,
        DeliveryMetrics
    {
    }
}

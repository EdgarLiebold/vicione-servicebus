namespace ViciOne.ServiceBus
{
    using Transports;


    public interface IEventHubRider :
        IRiderControl,
        IEventHubEndpointConnector
    {
        IEventHubProducerProvider GetProducerProvider(ConsumeContext consumeContext = default);
    }
}

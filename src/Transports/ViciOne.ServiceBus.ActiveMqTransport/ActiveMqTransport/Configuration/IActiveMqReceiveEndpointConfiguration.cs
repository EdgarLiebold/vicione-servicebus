namespace ViciOne.ServiceBus.ActiveMqTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;
    using Transports;


    public interface IActiveMqReceiveEndpointConfiguration :
        IReceiveEndpointConfiguration,
        IActiveMqEndpointConfiguration
    {
        ReceiveSettings Settings { get; }

        void Build(IHost host);
    }
}

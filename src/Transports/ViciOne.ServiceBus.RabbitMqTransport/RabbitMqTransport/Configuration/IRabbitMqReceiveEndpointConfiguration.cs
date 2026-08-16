namespace ViciOne.ServiceBus.RabbitMqTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;
    using Transports;


    public interface IRabbitMqReceiveEndpointConfiguration :
        IReceiveEndpointConfiguration,
        IRabbitMqEndpointConfiguration
    {
        ReceiveSettings Settings { get; }

        void Build(IHost host);
    }
}

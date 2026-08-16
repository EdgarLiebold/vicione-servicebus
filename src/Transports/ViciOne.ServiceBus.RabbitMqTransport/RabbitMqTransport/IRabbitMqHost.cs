namespace ViciOne.ServiceBus.RabbitMqTransport
{
    using Transports;


    public interface IRabbitMqHost :
        IHost<IRabbitMqReceiveEndpointConfigurator>
    {
    }
}

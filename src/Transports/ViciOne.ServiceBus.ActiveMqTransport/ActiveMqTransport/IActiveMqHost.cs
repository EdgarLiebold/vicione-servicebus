namespace ViciOne.ServiceBus.ActiveMqTransport
{
    using Transports;


    public interface IActiveMqHost :
        IHost<IActiveMqReceiveEndpointConfigurator>
    {
    }
}

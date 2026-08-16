namespace ViciOne.ServiceBus.ActiveMqTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface IActiveMqEndpointConfiguration :
        IEndpointConfiguration
    {
        new IActiveMqTopologyConfiguration Topology { get; }
    }
}

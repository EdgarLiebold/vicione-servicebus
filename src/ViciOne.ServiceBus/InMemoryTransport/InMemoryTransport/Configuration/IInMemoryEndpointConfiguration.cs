namespace ViciOne.ServiceBus.InMemoryTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface IInMemoryEndpointConfiguration :
        IEndpointConfiguration
    {
        new IInMemoryTopologyConfiguration Topology { get; }
    }
}

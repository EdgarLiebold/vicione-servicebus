namespace ViciOne.ServiceBus.SqlTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface ISqlEndpointConfiguration :
        IEndpointConfiguration
    {
        new ISqlTopologyConfiguration Topology { get; }
    }
}

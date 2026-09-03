namespace ViciOne.ServiceBus.SqlTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;
    using Transports;


    public interface ISqlReceiveEndpointConfiguration :
        IReceiveEndpointConfiguration,
        ISqlEndpointConfiguration
    {
        ReceiveSettings Settings { get; }

        void Build(IHost host);
    }
}

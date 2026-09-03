namespace ViciOne.ServiceBus.SqlTransport
{
    using Transports;


    public interface ISqlHost :
        IHost<ISqlReceiveEndpointConfigurator>
    {
    }
}

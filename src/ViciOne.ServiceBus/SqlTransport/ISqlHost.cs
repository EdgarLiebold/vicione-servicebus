using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

public interface ISqlHost :
    IHost<ISqlReceiveEndpointConfigurator>
{
}

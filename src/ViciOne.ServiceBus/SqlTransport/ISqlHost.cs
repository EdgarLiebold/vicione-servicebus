using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Defines the operations required by sql host.</summary>
public interface ISqlHost :
    IHost<ISqlReceiveEndpointConfigurator>
{
}

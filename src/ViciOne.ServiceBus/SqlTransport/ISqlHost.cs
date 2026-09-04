using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Defines the contract for sql host.
/// </summary>
public interface ISqlHost :
    IHost<ISqlReceiveEndpointConfigurator>
{
}

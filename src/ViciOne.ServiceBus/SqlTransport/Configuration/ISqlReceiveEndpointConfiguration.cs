using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

public interface ISqlReceiveEndpointConfiguration :
    IReceiveEndpointConfiguration,
    ISqlEndpointConfiguration
{
    ReceiveSettings Settings { get; }

    void Build(IHost host);
}

using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Defines sql receive endpoint configuration.</summary>
public interface ISqlReceiveEndpointConfiguration :
    IReceiveEndpointConfiguration,
    ISqlEndpointConfiguration
{
    /// <summary>Gets the settings.</summary>
    ReceiveSettings Settings { get; }

    /// <summary>Builds the configured component.</summary>
    /// <param name="host">The host.</param>
    void Build(IHost host);
}

using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Exposes the settings and build operation of an ActiveMQ receive endpoint.</summary>
public interface IActiveMqReceiveEndpointConfiguration :
    IReceiveEndpointConfiguration,
    IActiveMqEndpointConfiguration
{
    /// <summary>Gets the queue receive settings.</summary>
    ReceiveSettings Settings { get; }

    /// <summary>Builds and registers the endpoint with its host.</summary>
    /// <param name="host">The host that owns the endpoint.</param>
    void Build(IHost host);
}

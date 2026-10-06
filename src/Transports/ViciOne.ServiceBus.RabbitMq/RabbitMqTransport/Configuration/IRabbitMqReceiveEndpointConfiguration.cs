using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Builds a RabbitMQ receive endpoint from validated queue and pipeline settings.</summary>
public interface IRabbitMqReceiveEndpointConfiguration :
    IReceiveEndpointConfiguration,
    IRabbitMqEndpointConfiguration
{
    /// <summary>Gets the receive settings used to declare broker topology.</summary>
    ReceiveSettings Settings { get; }

    /// <summary>Builds and attaches the receive endpoint to a running host.</summary>
    /// <param name="host">The host that owns the receive endpoint.</param>
    void Build(IHost host);
}

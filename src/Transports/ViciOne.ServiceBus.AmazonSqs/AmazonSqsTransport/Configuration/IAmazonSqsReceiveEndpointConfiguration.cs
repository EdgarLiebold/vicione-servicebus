using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>Defines the queue settings and host-registration contract of an Amazon SQS receive endpoint.</summary>
public interface IAmazonSqsReceiveEndpointConfiguration :
    IReceiveEndpointConfiguration,
    IAmazonSqsEndpointConfiguration
{
    /// <summary>Gets the queue and receive settings.</summary>
    ReceiveSettings Settings { get; }

    /// <summary>Builds and registers the receive endpoint.</summary>
    /// <param name="host">The host that owns the endpoint.</param>
    void Build(IHost host);
}

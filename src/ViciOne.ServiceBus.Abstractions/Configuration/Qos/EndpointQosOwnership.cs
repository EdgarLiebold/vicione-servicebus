namespace ViciOne.ServiceBus.Configuration;

/// <summary>Identifies the configuration boundary that attempted to own endpoint transport QoS.</summary>
public enum EndpointQosOwnership
{
    /// <summary>The receive endpoint owns the transport settings.</summary>
    Endpoint = 0,
    /// <summary>A consumer definition attempted to own the transport settings.</summary>
    ConsumerDefinition = 1
}

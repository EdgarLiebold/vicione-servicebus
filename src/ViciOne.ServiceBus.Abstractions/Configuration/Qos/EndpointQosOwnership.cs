namespace ViciOne.ServiceBus.Configuration;

/// <summary>Identifies the configuration boundary that attempted to own endpoint transport QoS.</summary>
public enum EndpointQosOwnership
{
    /// <summary>Indicates endpoint.</summary>
    Endpoint = 0,
    /// <summary>Indicates consumer definition.</summary>
    ConsumerDefinition = 1
}

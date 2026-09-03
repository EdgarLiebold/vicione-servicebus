namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Identifies the configuration boundary that attempted to own endpoint transport QoS.
/// </summary>
public enum EndpointQosOwnership
{
    Endpoint = 0,
    ConsumerDefinition = 1
}

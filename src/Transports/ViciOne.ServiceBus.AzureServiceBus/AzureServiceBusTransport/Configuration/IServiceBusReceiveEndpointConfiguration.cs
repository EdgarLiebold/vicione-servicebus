namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Defines the contract for service bus receive endpoint configuration.
/// </summary>
public interface IServiceBusReceiveEndpointConfiguration :
    IServiceBusEntityEndpointConfiguration
{
    /// <summary>
    /// Gets the settings value.
    /// </summary>
    ReceiveSettings Settings { get; }
}

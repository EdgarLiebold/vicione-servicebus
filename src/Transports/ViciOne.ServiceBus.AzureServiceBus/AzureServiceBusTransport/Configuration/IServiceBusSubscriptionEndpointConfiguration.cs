namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Defines the contract for service bus subscription endpoint configuration.
/// </summary>
public interface IServiceBusSubscriptionEndpointConfiguration :
    IServiceBusEntityEndpointConfiguration
{
    /// <summary>
    /// Gets the settings value.
    /// </summary>
    SubscriptionSettings Settings { get; }
}

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Exposes the settings for an Azure Service Bus subscription receive endpoint.</summary>
public interface IServiceBusSubscriptionEndpointConfiguration :
    IServiceBusEntityEndpointConfiguration
{
    /// <summary>Gets the topic, subscription, and processor settings.</summary>
    SubscriptionSettings Settings { get; }
}

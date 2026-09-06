namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Exposes the settings for an Azure Service Bus queue receive endpoint.</summary>
public interface IServiceBusReceiveEndpointConfiguration :
    IServiceBusEntityEndpointConfiguration
{
    /// <summary>Gets the queue entity and processor settings.</summary>
    ReceiveSettings Settings { get; }
}

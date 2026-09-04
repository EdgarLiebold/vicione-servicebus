namespace ViciOne.ServiceBus.AzureServiceBusTransport.Configuration;

public interface IServiceBusSubscriptionEndpointConfiguration :
    IServiceBusEntityEndpointConfiguration
{
    SubscriptionSettings Settings { get; }
}

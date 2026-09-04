namespace ViciOne.ServiceBus.AzureServiceBusTransport.Configuration;

public interface IServiceBusReceiveEndpointConfiguration :
    IServiceBusEntityEndpointConfiguration
{
    ReceiveSettings Settings { get; }
}

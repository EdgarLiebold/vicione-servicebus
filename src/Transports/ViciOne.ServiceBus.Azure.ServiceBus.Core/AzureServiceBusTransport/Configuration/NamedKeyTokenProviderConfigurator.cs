using Azure;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Configuration;

public class NamedKeyTokenProviderConfigurator :
    IServiceBusNamedKeyTokenProviderConfigurator
{
    public AzureNamedKeyCredential NamedKeyCredential { get; set; } = null!;
}

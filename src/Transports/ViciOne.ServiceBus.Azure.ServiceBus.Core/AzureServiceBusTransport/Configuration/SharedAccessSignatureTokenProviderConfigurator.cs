using Azure;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Configuration;

public class SharedAccessSignatureTokenProviderConfigurator :
    ISharedAccessSignatureTokenProviderConfigurator
{
    public AzureSasCredential SasCredential { get; set; } = null!;
}

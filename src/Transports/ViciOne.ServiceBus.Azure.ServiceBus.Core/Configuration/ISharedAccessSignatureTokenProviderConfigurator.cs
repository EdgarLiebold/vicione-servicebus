using Azure;

namespace ViciOne.ServiceBus;

public interface ISharedAccessSignatureTokenProviderConfigurator
{
    AzureSasCredential SasCredential { set; }
}

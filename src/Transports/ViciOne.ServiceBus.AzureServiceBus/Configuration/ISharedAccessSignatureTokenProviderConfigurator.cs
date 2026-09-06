using Azure;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures Azure Service Bus authentication with a shared-access signature credential.</summary>
public interface ISharedAccessSignatureTokenProviderConfigurator
{
    /// <summary>Sets the shared-access signature credential.</summary>
    AzureSasCredential SasCredential { set; }
}

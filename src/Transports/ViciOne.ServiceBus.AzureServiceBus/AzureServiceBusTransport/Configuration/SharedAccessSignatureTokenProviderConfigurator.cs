using Azure;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Captures the shared-access signature credential used to authenticate Azure Service Bus clients.</summary>
public class SharedAccessSignatureTokenProviderConfigurator :
    ISharedAccessSignatureTokenProviderConfigurator
{
    /// <summary>Gets or sets the shared-access signature credential.</summary>
    public AzureSasCredential SasCredential { get; set; } = null!;
}

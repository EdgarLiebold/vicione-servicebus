using Azure;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Provides a shared access signature token provider configurator implementation.
/// </summary>
public class SharedAccessSignatureTokenProviderConfigurator :
    ISharedAccessSignatureTokenProviderConfigurator
{
    /// <summary>
    /// Gets or sets the sas credential value.
    /// </summary>
    public AzureSasCredential SasCredential { get; set; } = null!;
}

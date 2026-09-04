using Azure;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for shared access signature token provider configurator.
/// </summary>
public interface ISharedAccessSignatureTokenProviderConfigurator
{
    /// <summary>
    /// Gets or sets the sas credential value.
    /// </summary>
    AzureSasCredential SasCredential { set; }
}

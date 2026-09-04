using Azure;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for service bus named key token provider configurator.
/// </summary>
public interface IServiceBusNamedKeyTokenProviderConfigurator
{
    /// <summary>
    /// Gets or sets the named key credential value.
    /// </summary>
    AzureNamedKeyCredential NamedKeyCredential { set; }
}

using Azure;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Provides a named key token provider configurator implementation.
/// </summary>
public class NamedKeyTokenProviderConfigurator :
    IServiceBusNamedKeyTokenProviderConfigurator
{
    /// <summary>
    /// Gets or sets the named key credential value.
    /// </summary>
    public AzureNamedKeyCredential NamedKeyCredential { get; set; } = null!;
}

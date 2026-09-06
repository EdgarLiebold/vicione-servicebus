using Azure;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Captures the shared-access key credential used to authenticate Azure Service Bus clients.</summary>
public class NamedKeyTokenProviderConfigurator :
    IServiceBusNamedKeyTokenProviderConfigurator
{
    /// <summary>Gets or sets the shared-access key credential.</summary>
    public AzureNamedKeyCredential NamedKeyCredential { get; set; } = null!;
}

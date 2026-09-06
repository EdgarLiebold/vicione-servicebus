using Azure;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures Azure Service Bus authentication with a shared-access key credential.</summary>
public interface IServiceBusNamedKeyTokenProviderConfigurator
{
    /// <summary>Sets the shared-access key credential.</summary>
    AzureNamedKeyCredential NamedKeyCredential { set; }
}

using Azure;

namespace ViciOne.ServiceBus;

public interface IServiceBusNamedKeyTokenProviderConfigurator
{
    AzureNamedKeyCredential NamedKeyCredential { set; }
}

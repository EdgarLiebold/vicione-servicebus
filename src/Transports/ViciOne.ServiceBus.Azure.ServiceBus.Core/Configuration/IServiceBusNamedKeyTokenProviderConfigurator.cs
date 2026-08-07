// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using Azure;


    public interface IServiceBusNamedKeyTokenProviderConfigurator
    {
        AzureNamedKeyCredential NamedKeyCredential { set; }
    }
}

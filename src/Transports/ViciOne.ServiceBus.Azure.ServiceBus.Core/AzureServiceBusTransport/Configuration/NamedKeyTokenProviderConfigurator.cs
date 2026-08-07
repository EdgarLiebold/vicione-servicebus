// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureServiceBusTransport.Configuration
{
    using Azure;


    public class NamedKeyTokenProviderConfigurator :
        IServiceBusNamedKeyTokenProviderConfigurator
    {
        public AzureNamedKeyCredential NamedKeyCredential { get; set; }
    }
}
